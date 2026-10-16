using FernApi;
using Environment = FernApi.Http.Environment;

var client = new FernApiClient();

var response = await client.Widgets.ListWidgetsAsync();

Console.WriteLine(response);
