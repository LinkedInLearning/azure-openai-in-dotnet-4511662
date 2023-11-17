using System.ComponentModel;
using System.Text.Json;
using System.Net.Http;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SemanticFunctions;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;



public static class DownloadSummarize {
    public static async Task<Dictionary<string,string>> Execute(IKernel kernel) {
        //*********************************************************************
        //Import native function
        //*********************************************************************
        string plugInName = "NativeFunction";
        NativeFunctions nativeFunctions = new NativeFunctions();
        kernel.ImportFunctions(nativeFunctions, plugInName);
        ISKFunction skFunction = kernel.Functions.GetFunction(plugInName, "DownloadContentFromUrl");

        //*********************************************************************
        //Define semantic function inline
        //*********************************************************************
        string skPrompt = @"Summarize the provided unstructured text in 3 easy to understand sentences. 
                            The sentences need to be short and provide the most important content of the provided text.
                            Text to summarize: {{$input}}";

        OpenAIRequestSettings openAIRequestSettings = new OpenAIRequestSettings
        {
            MaxTokens = 100,
            Temperature = 0.7,
            TopP = 1,
            FrequencyPenalty = 0,
            PresencePenalty = 0,
            StopSequences = new List<string> { "\n" }
        };

        PromptTemplateConfig promptTemplateConfig = new PromptTemplateConfig(); 
        promptTemplateConfig.ModelSettings.Add(openAIRequestSettings); 

        PromptTemplate promptTemplate = new PromptTemplate(
            skPrompt, 
            promptTemplateConfig, 
            kernel
        );

        SemanticFunctionConfig semanticFunctionConfig = new SemanticFunctionConfig(promptTemplateConfig, promptTemplate); 
        plugInName = "SemanticFunctions";
        string functionName = "SummarizeText"; 
        ISKFunction summaryFunction = kernel.RegisterSemanticFunction(plugInName, functionName, semanticFunctionConfig); 

        //*********************************************************************
        //Execute functions (download article & summarize article)
        //*********************************************************************
        Dictionary<string,string> downloads = new Dictionary<string,string>() {
            {
                "1; Champions League",
                "https://en.wikipedia.org/w/api.php?action=query&format=json&prop=extracts&titles=Champions%20League&explaintext=1&exsectionformat=plain"
            },
            {
                "2; Super Bowl",
                "https://en.wikipedia.org/w/api.php?action=query&format=json&prop=extracts&titles=Super%20Bowl&explaintext=1&exsectionformat=plain"
            },
            {
                "3; Cricket World Cup",
                "https://en.wikipedia.org/w/api.php?action=query&format=json&prop=extracts&titles=Cricket%20World%20Cup&explaintext=1&exsectionformat=plain"
            }
        };

        Dictionary<string,string> summarizations = new Dictionary<string,string>(); 

        foreach (var downloadUrl in downloads) {
            //Execute native function (Download article)
            ContextVariables contextVariables = new ContextVariables();
            contextVariables.Add("url", downloadUrl.Value);
            contextVariables.Add("maxSize", "5000");

            plugInName = "NativeFunction";
            functionName = "DownloadContentFromUrl";
            skFunction = kernel.Functions.GetFunction(plugInName, functionName);
            KernelResult kernelResult = await kernel.RunAsync(contextVariables, skFunction);         
            string wikiArticle = kernelResult.GetValue<string>() ?? "";
            Console.WriteLine($"Content downloaded from {downloadUrl.Value}...");

            //Execute semantic function (Summarize downloaded article)
            plugInName = "SemanticFunctions";
            functionName = "SummarizeText"; 
            skFunction = kernel.Functions.GetFunction(plugInName, functionName);
            kernelResult = await kernel.RunAsync(wikiArticle, skFunction);
            string summarization = kernelResult.GetValue<string>() ?? "";
            Console.WriteLine($"Summarized content from {downloadUrl.Value}...");
            
            summarizations.Add(String.Concat(downloadUrl.Key, ";", downloadUrl.Value), summarization);
        }
        
        return summarizations;
    }

}

public class NativeFunctions {

    [SKFunction, Description("Download content from url")]
    public async Task<string> DownloadContentFromUrl(string url, int maxSize = 1000) 
    {
        using HttpClient httpClient = new HttpClient();
        
        HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(url);
        JsonDocument jsonDocument = JsonDocument.Parse(await httpResponseMessage.Content.ReadAsStringAsync());

        JsonElement jsonElement = jsonDocument.RootElement; 
        string article = IterateJson(jsonElement);
        return article.Length > maxSize ? IterateJson(jsonElement).Substring(0, maxSize) : article; 
    }

    string IterateJson(JsonElement root) 
    {
        string article = ""; 

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement element in root.EnumerateArray())
            {
                IterateJson(element);
            }
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (property.Name == "extract"){
                    article = property.Value.GetString()??"";
                    break; 
                }
                article = IterateJson(property.Value);
            }
        }
        return article; 
    }
}