# Retrofit Core

1. No build step
2. Dynamic Service Proxy generator
3. Support for Header as property

# Example
```c#

public interface IBackendService {

    // when set, it will always be sent with
    // every request
    [Header("access-key")]
    AccessKey { get; set; }

    [Get("/location/{ip}")]
    Task<IPInfo> GetLocationInfoAsync([Path("ip")] string ip);

    [Post("/location/{ip}")]
    Task<IPInfo> SaveLocationInfoAsync([Body] IPInfo info);
    
    // retrive http response for detailed response
    [Get("/video/{id}.mp4")]
    Task<HttpResponseMessage> GetRawResponseAsync([Query("id")] string id);
    
    [Get("/voice/{id}.mp3")]
    Task<byte[]> GetByteArrayAsync([Query("id")] string id);
    
    // Response Object with Header
    [Get("/projects")]
    Task<GitLabResponse<GitLabProject>> GetProjectsAsync();
}

public class GitLabResponse<T>: ApiResponse<T[]> {

   [Header("x-total-pages")]
   public int TotalPages {get;set;}

}

```

# Usage
```c#

    var client = RetroClient.Create<IBackendService, BaseService>( new Uri("base url...") , httpClient);

```
