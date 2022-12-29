using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace SpeakOutLoud
{
    public static class SpeechMaker
    {
        [FunctionName("Speak")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Get the text to convert from the query string or request body
            string text = req.Query["text"];
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            text = text ?? data?.text;

            if (string.IsNullOrEmpty(text))
            {
                return new BadRequestObjectResult("Please provide a text to convert in the 'text' query string or request body");
            }

            // Feel free to bring these from external config.  This will do for this simple demo.
            var yourSubscriptionKey = "your-cognitive-services-key-goes-here";
            var yourServiceRegion = "your-service-region";

            // Convert the text to speech
            byte[] audioData;

            var speechConfig = SpeechConfig.FromSubscription(yourSubscriptionKey, yourServiceRegion);
            using (var synthesizer = new SpeechSynthesizer(speechConfig))
            {
                // Synthesize the text to speech
                using (var result = await synthesizer.SpeakTextAsync(text))
                {
                    if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                    {
                        audioData = result.AudioData;
                    }
                    else
                    {
                        return new BadRequestObjectResult($"Failed to convert text to speech: {result.Reason}");
                    }
                }
            }

            // Return the audio data as a binary stream
            return new FileContentResult(audioData, "audio/x-wav");
        }
    }
}
