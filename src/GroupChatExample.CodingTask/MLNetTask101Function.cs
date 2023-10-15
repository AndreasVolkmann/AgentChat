﻿using Azure.AI.OpenAI;
using GroupChatExample.Helper;
using FluentAssertions.Equivalency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;

namespace GroupChatExample.CodingTask
{
    public partial class MLNetExample101Function
    {
        private HttpClient _httpClient;

        public MLNetExample101Function(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// fix error code.
        /// </summary>
        /// <param name="code">code with error</param>
        /// <param name="errorMessage">error message.</param>
        /// <param name="k">number of example to return. default is 5.</param>
        /// <param name="threshold">score thresold. default is 0.8</param>
        [FunctionAttribution]
        public async Task<string> FixError(string code, string errorMessage, int k = 5, float threshold = 0.8f)
        {
            var result = await QueryAsync(code, k, threshold);
            // if no result is found, return it
            if (result.StartsWith("No example found"))
            {
                return result;
            }

            // else, use llm to summarize the result
            var agent = new GPTAgent(
                Constant.GPT,
                Constant.GPT_4_MODEL_ID,
                "admin",
                @$"Fix the error of given code and explain how you fix it. Put your answer between ```csharp and ```
Say you don't know how to fix the error if provided reference is not helpful. Please think step by step.

# MLNet Reference
{result}
# End

#code with error#
{code}

#Error Message#
{errorMessage}

Example response:
According to MLNet reference, the error is caused by xxx. Here's the fix code
```csharp
// fixed code.
```
--- or ---
I don't know how to fix this error as MLNet reference is not helpful.
-----
");
            var response = await agent.CallAsync(Enumerable.Empty<ChatMessage>());

            if (response is null)
            {
                throw new Exception("response is null");
            }

            return response.Content;
        }

        /// <summary>
        /// search mlnet api examples.
        /// </summary>
        /// <param name="step">step to search</param>
        /// <param name="k">number of example to return, default is 5.</param>
        /// <param name="threshold">score thresold. default is 0.7</param>
        [FunctionAttribution]
        public async Task<string> SearchMLNetApiExample(string step, int k = 5, float threshold = 0.7f)
        {
            var result = await QueryAsync(step, k, threshold);
            // if no result is found, return it
            if (result.StartsWith("No example found"))
            {
                return result;
            }

            // else, use llm to summarize the result
            var agent = new GPTAgent(
                Constant.GPT,
                Constant.GPT_4_MODEL_ID,
                "admin",
                @$"You create several mlnet example from reference to resolve given step. Put your answer between ```csharp and ```
Say you don't have example if provided reference is not helpful. Please think step by step.

# Reference
{result}
# End

#Step#
{step}
#EndStep#

Example response:
```csharp
// example1
// example2
// example3
```
---
I don't have example for this step.
---
");
            var response = await agent.CallAsync(Enumerable.Empty<ChatMessage>());

            if (response is null)
            {
                throw new Exception("response is null");
            }

            return response.Content;
        }


        private async Task<string> QueryAsync(string query, int k = 5, float threshold = 0.7f)
        {
            var baseUri = "https://littlelittlecloud-mlnet-samples.hf.space/--replicas/kkvq6/api/search";
            var documentID = "mlnet_notebook_examples_v1.json";
            var data = new
            {
                data = new object[]
                {
                    query,
                    documentID,
                    k,
                    threshold,
                },
            };

            var content = JsonSerializer.Serialize(data);
            var bearToken = Constant.MLNET101SEARCHTOEKN;
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearToken);

            var response = await _httpClient.PostAsync(baseUri, new StringContent(content, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                return $"Error: {response.StatusCode}";
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            var res = JsonSerializer.Deserialize<Response>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });

            var recordJsonList = res?.Data ?? Array.Empty<string>();
            if (recordJsonList.Length == 0)
            {
                return $"No example found for {query}";
            }

            var records = JsonSerializer.Deserialize<List<Record>>(recordJsonList[0], new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            }) ?? new List<Record>();

            if (records.Count == 0)
            {
                return $"No example found for {query}";
            }

            try
            {
                var sb = new StringBuilder();
                foreach (var record in records)
                {
                    sb.AppendLine($"## ML.Net example ##");
                    sb.AppendLine(record.Content.ToString());
                    sb.AppendLine($"## End of ML.Net example ##");
                }

                return sb.ToString();
            }
            catch (Exception)
            {
                return $"No example found for {query}";
            }
        }

        private class Record
        {
            public string Content { get; set; } = "";

            [JsonPropertyName("meta_data")]
            public Dictionary<string, object> MetaData { get; set; } = new Dictionary<string, object>();
        }

        class Response
        {
            public string[] Data { get; set; } = Array.Empty<string>();
        }
    }
}
