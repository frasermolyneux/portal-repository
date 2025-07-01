namespace XtremeIdiots.Portal.Repository.Api.V1.Extensions
{
    public static class HttpClientExtensions
    {
        public static async Task DownloadFileTaskAsync(this HttpClient client, Uri uri, string FileName)
        {
            using (var s = await client.GetStreamAsync(uri))
            {
                using (var fs = new FileStream(FileName, FileMode.OpenOrCreate))
                {
                    await s.CopyToAsync(fs);
                }
            }
        }
    }
}
