<Query Kind="Program">
  <NuGetReference>OpenAI-DotNet</NuGetReference>
  <Namespace>OpenAI</Namespace>
  <Namespace>OpenAI.Assistants</Namespace>
  <Namespace>OpenAI.Audio</Namespace>
  <Namespace>OpenAI.Batch</Namespace>
  <Namespace>OpenAI.Chat</Namespace>
  <Namespace>OpenAI.Embeddings</Namespace>
  <Namespace>OpenAI.Extensions</Namespace>
  <Namespace>OpenAI.Files</Namespace>
  <Namespace>OpenAI.FineTuning</Namespace>
  <Namespace>OpenAI.Images</Namespace>
  <Namespace>OpenAI.Models</Namespace>
  <Namespace>OpenAI.Moderations</Namespace>
  <Namespace>OpenAI.Realtime</Namespace>
  <Namespace>OpenAI.Threads</Namespace>
  <Namespace>OpenAI.VectorStores</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

using OpenAI;

// This LINQPad app deletes all the assistants, files and vector stores from your OpenAI dashboard
// It has been tested with only a handful of entries. Adjust the code if not working correctly for many entries
// Author: jentel at hotmail dot com

async Task Main()
{
	string ApiKey = "your OpenAI api key";
	string BaseUrl = "https://api.openai.com/v1/";

	
	using (HttpClient client = new HttpClient())
	{
		// Set the Authorization header with your API key.
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

		// Delete files from OpenAI
		await DeleteFilesAsync(client, BaseUrl);
	}

	var openAIClient = new OpenAIClient(ApiKey);
	DeleteAssistants(openAIClient);
	DeleteAllVectorStoresAsync(openAIClient);

	Console.WriteLine("Operation completed.");
	
}



public async Task DeleteAllVectorStoresAsync(OpenAIClient client)
{
	try
    {
        bool hasMore = true;
        string lastVectorStoreId = null;

        while (hasMore)
        {
            // Create query parameters for pagination
            var queryParams = new ListQuery(limit: 100, after: lastVectorStoreId);

			// Get a page of assistants
			var vectorStores = await client.VectorStoresEndpoint.ListVectorStoresAsync(queryParams);

			// Delete all assistants in the current page
			foreach (var vectorStore in vectorStores.Items)
			{
				await client.VectorStoresEndpoint.DeleteVectorStoreAsync(vectorStore.Id);
				Console.WriteLine($"Deleted vector store: {vectorStore.Id}");
			}

			// Update pagination control variables
			hasMore = vectorStores.HasMore;
			lastVectorStoreId = vectorStores.Items.LastOrDefault()?.Id;
		}

		Console.WriteLine("All vector stores deleted successfully.");
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Error: {ex.Message}");
	}
}


async void DeleteAssistants(OpenAIClient client)
{
	 try
    {
        bool hasMore = true;
        string lastAssistantId = null;

        while (hasMore)
        {
            // Create query parameters for pagination
            var queryParams = new ListQuery(limit: 100, after: lastAssistantId);
            
            // Get a page of assistants
            var assistants = await client.AssistantsEndpoint.ListAssistantsAsync(queryParams);
            
            // Delete all assistants in the current page
            foreach (var assistant in assistants.Items)
			{
				await client.AssistantsEndpoint.DeleteAssistantAsync(assistant.Id);
				Console.WriteLine($"Deleted assistant: {assistant.Id}");
			}

			// Update pagination control variables
			hasMore = assistants.HasMore;
			lastAssistantId = assistants.Items.LastOrDefault()?.Id;
		}

		Console.WriteLine("All assistants deleted successfully");
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Error: {ex.Message}");
	}

}

static async Task DeleteFilesAsync(HttpClient client, string baseUrl)
{
	string listFilesUrl = baseUrl + "files";
	Console.WriteLine("Retrieving list of files...");

	HttpResponseMessage listResponse = await client.GetAsync(listFilesUrl);
	if (!listResponse.IsSuccessStatusCode)
	{
		Console.WriteLine($"Error retrieving files: {listResponse.StatusCode}");
		return;
	}

	string listResponseContent = await listResponse.Content.ReadAsStringAsync();
	try
	{
		// The response is expected to be JSON with a "data" array containing file objects.
		using (JsonDocument document = JsonDocument.Parse(listResponseContent))
		{
			if (document.RootElement.TryGetProperty("data", out JsonElement filesElement) &&
				filesElement.ValueKind == JsonValueKind.Array)
			{
				foreach (JsonElement fileElement in filesElement.EnumerateArray())
				{
					if (fileElement.TryGetProperty("id", out JsonElement idElement))
					{
						string fileId = idElement.GetString();
						Console.WriteLine($"Deleting file with ID: {fileId}...");
						await DeleteFileAsync(client, fileId, baseUrl);
					}
				}
			}
			else
			{
				Console.WriteLine("No files found in the response.");
			}
		}
	}
	catch (JsonException ex)
	{
		Console.WriteLine($"Error parsing JSON response: {ex.Message}");
	}
}

/// <summary>
/// Deletes a single file using the OpenAI API.
/// </summary>
/// <param name="client">An HttpClient instance with your API key set.</param>
/// <param name="fileId">The ID of the file to delete.</param>
private static async Task DeleteFileAsync(HttpClient client, string fileId, string baseUrl)
{
	string deleteUrl = $"{baseUrl}files/{fileId}";
	HttpResponseMessage deleteResponse = await client.DeleteAsync(deleteUrl);

	if (deleteResponse.IsSuccessStatusCode)
	{
		Console.WriteLine($"Successfully deleted file: {fileId}");
	}
	else
	{
		Console.WriteLine($"Failed to delete file: {fileId}. Status code: {deleteResponse.StatusCode}");
		string errorContent = await deleteResponse.Content.ReadAsStringAsync();
		Console.WriteLine("Error details: " + errorContent);
	}
}


