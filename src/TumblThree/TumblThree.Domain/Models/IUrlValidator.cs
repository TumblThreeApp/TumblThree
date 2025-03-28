namespace TumblThree.Domain.Models
{
    public interface IUrlValidator
    {
        bool IsValidTumblrHiddenUrl(string url);

        bool IsValidTumblrLikedByUrl(string url);

        bool IsValidTumblrSearchUrl(string url);

        bool IsValidTumblrTagSearchUrl(string url);

        bool IsValidTumblrUrl(string url);

        string AddHttpsProtocol(string url);

        bool IsTumbexUrl(string url);

        bool IsValidTumblrLikesUrl(string url);

        bool IsValidUrl(string url);

        bool IsValidTwitterUrl(string url);

        bool IsValidNewTumblUrl(string url);

        bool IsValidBlueskyUrl(string url);
    }
}
