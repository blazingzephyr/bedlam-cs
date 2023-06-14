
using Discord;
using Notion.Client;

namespace Bedlam;

internal class NotionManager
{
    public record struct NotionManagerOptions(int Interval);
    public record struct SearchResponse
    (bool IsSuccessful, List<IObject>? Results = null, NotionApiException? Exception = null);

    public delegate void NotionEventHandler
    (NotionManager source);

    public delegate void NotionApiErrorEventHandler
    (NotionManager source, NotionApiException exception);

    public delegate void NotionUpdatedEventHandler
    (NotionManager source, List<IObject> updated, List<IObject> items);

    public event NotionEventHandler? OnReady;
    public event NotionApiErrorEventHandler? OnApiError;
    public event NotionUpdatedEventHandler? OnUpdated;

    public NotionClient Client { get; }
    readonly NotionManagerOptions _options;

    public NotionManager(string integrationToken, NotionManagerOptions options)
    {
        ClientOptions clientOptions = new () { AuthToken = integrationToken }; 
        Client = NotionClientFactory.Create(clientOptions)
        ?? throw new NullReferenceException("client");

        this._options = options;
        OnReady?.Invoke(this);
    }

    public async Task Watch(CancellationToken token)
    {
        Dictionary<string, DateTime>? current = null;

        while (!token.IsCancellationRequested)
        {
            var response = await Search(token);
            if (!response.IsSuccessful)
            {
                OnApiError?.Invoke(this, response.Exception!);
            }
            else if (!token.IsCancellationRequested)
            {
                if (current == null)
                {
                    current = new Dictionary<string, DateTime>();
                    for (int i = 0; i < response.Results!.Count; i++)
                    {
                        IObject item = response.Results[i];
                        DateTime lastEditedTime = item switch
                        {
                            Page page => page.LastEditedTime,
                            Database database => database.LastEditedTime,
                            _ => throw new InvalidCastException()
                        };

                        current.Add(item.Id, lastEditedTime);
                    }
                }
                else
                {
                    List<IObject> updated = new ();
                    foreach (var item in response.Results!)
                    {
                        (bool relevant, Optional<DateTime> lastEditedTime) = item.Id switch
                        {
                            string id when current.TryGetValue(id, out DateTime old) => item switch
                            {
                                Page page => (page.LastEditedTime == old, page.LastEditedTime),
                                Database db => (db.LastEditedTime == old, db.LastEditedTime),
                                _ => throw new InvalidCastException()
                            },
                            _ => (false, Optional<DateTime>.Unspecified)
                        };
                        
                        if (!relevant)
                        {
                            current[item.Id] = lastEditedTime.Value;
                            updated.Add(item);
                        }
                    }

                    if (updated.Count > 0)
                    {
                        OnUpdated?.Invoke(this, updated, response.Results);
                    }
                }
            }

            await Task.Delay(_options.Interval, token);
        }
    }

    public async Task<SearchResponse> Search(CancellationToken token)
    {
        try
        {
            bool hasMore = true;
            var param = new SearchParameters
            {
                Sort = new SearchSort
                {
                    Direction = SearchDirection.Descending,
                    Timestamp = "last_edited_time"
                }
            };

            token.Register(() =>
            {
                hasMore = false;
            });

            List<IObject> results = new();
            while (hasMore)
            {
                var list = await Client.Search.SearchAsync(param);
                hasMore = list.HasMore;
                param.StartCursor = list.NextCursor;
                results.AddRange(list.Results);
            }

            return new SearchResponse
            (true, Results: token.IsCancellationRequested ? null : results);
        }
        catch (NotionApiException exception)
        {
            return new SearchResponse(false, Exception: exception);
        }
    }
}