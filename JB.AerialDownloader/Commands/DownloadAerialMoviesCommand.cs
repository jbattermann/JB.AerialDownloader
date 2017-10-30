using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using JB.AerialDownloader.Models;
using JB.AerialDownloader.Models.Apple;
using JB.AerialDownloader.Options;
using Jil;

namespace JB.AerialDownloader.Commands
{
    public class DownloadAerialMoviesCommand : ICommand<DownloadAerialMoviesOptions>
    {
        /// <summary>
        /// Executes the command and returns the corresponding exit code.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<int> ExecuteAndReturnExitCode(DownloadAerialMoviesOptions options,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(options.Output)) throw new ArgumentOutOfRangeException(nameof(options), $"{nameof(DownloadAerialMoviesOptions.Output)} may not be empty.");
            if (string.IsNullOrWhiteSpace(options.JsonUrl)) throw new ArgumentOutOfRangeException(nameof(options), $"{nameof(DownloadAerialMoviesOptions.JsonUrl)} may not be empty.");
            if (Uri.IsWellFormedUriString(options.JsonUrl, UriKind.Absolute) == false) throw new ArgumentOutOfRangeException(nameof(options), $"{nameof(DownloadAerialMoviesOptions.JsonUrl)} must be a valid URL.");

            var outputDirectoryInfo = new DirectoryInfo(Helpers.NormalizePotentialRelativeToFullPath(Helpers.NormalizeTargetPath(options.Output)));
            if (outputDirectoryInfo.Exists == false) throw new ArgumentOutOfRangeException(nameof(options), $"{nameof(DownloadAerialMoviesOptions.Output)} ({outputDirectoryInfo.FullName}) must exist.");


            var errorOccured = false;

            var aerialVideos = await GetVideosFromJsonUrl(options.JsonUrl, cancellationToken)
                .ConfigureAwait(false);

            var aerialVideoUrlsMatchingOptions = aerialVideos
                .SelectMany(aerialVideo => aerialVideo.VideoDownloadUrls)
                .Where(aerialVideoDownloadUrl => aerialVideoDownloadUrl.Quality.HasFlag(options.Quality))
                .Select(aerialVideoDownloadUrl => aerialVideoDownloadUrl.Url.ToString())
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .OrderBy(aerialVideoDownloadUrl => aerialVideoDownloadUrl, StringComparer.InvariantCultureIgnoreCase)
                .ToList();

            if (aerialVideoUrlsMatchingOptions.Count == 0)
            {
                Console.WriteLine($"The {nameof(DownloadAerialMoviesOptions.JsonUrl)} does not contain any videos (either not at all or not matching the {nameof(DownloadAerialMoviesOptions.Quality)} parameter)");
                return Constants.DefaultSuccessCode;
            }
            
            var downloadAerialVideoBlock = new ActionBlock<string>(async aerialVideoUrl =>
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        await DownloadVideo(aerialVideoUrl, options.Output, options.Force, cancellationToken).ConfigureAwait(false);
                        Console.Write(".");
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        errorOccured = true;

                        var currentForeColor = Console.ForegroundColor;
                        try
                        {
                            Console.WriteLine();
                            Console.ForegroundColor = ConsoleColor.Red;

                            Console.WriteLine("Error Downloading '{0}': {1}", aerialVideoUrl, exception.Message);
                        }
                        finally
                        {
                            Console.ForegroundColor = currentForeColor;
                        }
                    }
                },
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = (options.MaxDegreeOfParallelism <= 0 ? 1 : options.MaxDegreeOfParallelism),
                    CancellationToken = cancellationToken
                });

            foreach (var aerialVideoUrl in aerialVideoUrlsMatchingOptions)
            {
                downloadAerialVideoBlock.Post(aerialVideoUrl);
            }
            downloadAerialVideoBlock.Complete();

            await downloadAerialVideoBlock.Completion.ConfigureAwait(false);

            return errorOccured
                ? 1
                : 0;
        }

        /// <summary>
        /// Downloads the video from the provided <paramref name="videoUrl"/> into the <paramref name="targetDirectory"/>.
        /// </summary>
        /// <param name="videoUrl">The video URL.</param>
        /// <param name="targetDirectory">The target directory.</param>
        /// <param name="overwriteExistingFile">if set to <c>true</c> [overwrite existing file].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        private async Task DownloadVideo(string videoUrl, string targetDirectory, bool overwriteExistingFile,
                    CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var videoUri = new Uri(videoUrl, UriKind.Absolute);
            var videoFilename = videoUri.Segments.LastOrDefault() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(videoFilename) || videoFilename.EndsWith("/") || videoFilename.EndsWith("\\"))
                return; // invalid url

            var outputFileInfo = new FileInfo(Path.Combine(targetDirectory, videoFilename));
            if (outputFileInfo.Exists && overwriteExistingFile == false)
                return;

            cancellationToken.ThrowIfCancellationRequested();

            using (var httpClient = new HttpClient())
            {
                using (var httpHeaderResponseMessage = await httpClient
                    .GetAsync(videoUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (httpHeaderResponseMessage.IsSuccessStatusCode == false)
                        return;

                    if (outputFileInfo.Exists == false || httpHeaderResponseMessage.Content.Headers.ContentLength != outputFileInfo.Length)
                    {
                        try
                        {
                            using (var httpContentResponseMessage = await httpClient
                                .GetAsync(videoUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                                .ConfigureAwait(false))
                            {
                                using (var outputFileStream = File.OpenWrite(outputFileInfo.FullName))
                                {
                                    cancellationToken.ThrowIfCancellationRequested();

                                    await httpContentResponseMessage.Content.CopyToAsync(outputFileStream)
                                        .ConfigureAwait(false);
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            outputFileInfo.Refresh();
                            if (outputFileInfo.Exists)
                            {
                                try
                                {
                                    outputFileInfo.Delete();
                                }
                                catch (Exception)
                                {
                                    // ignore it at this stage
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the videos from the provided <paramref name="jsonUrl"/>.
        /// </summary>
        /// <param name="jsonUrl">The json URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        private async Task<ICollection<AerialVideo>> GetVideosFromJsonUrl(string jsonUrl, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = new List<AerialVideo>();
            var desrializationOptions = Jil.Options.ExcludeNullsIncludeInheritedCamelCase;

            using (var httpClient = new HttpClient())
            {
                using (var httpResponseMessage = await httpClient
                    .GetAsync(jsonUrl, HttpCompletionOption.ResponseContentRead, cancellationToken)
                    .ConfigureAwait(false))
                {
                    var jsonString = await httpResponseMessage.Content.ReadAsStringAsync();

                    // first try non-versioned assets, such has http://a1.phobos.apple.com/us/r1000/000/Features/atv/AutumnResources/videos/entries.json
                    NonVersionedAssets:
                    {
                        IdAndAssets[] idsAndAssets = null;
                        try
                        {
                            idsAndAssets = Jil.JSON.Deserialize<IdAndAssets[]>(jsonString, desrializationOptions);

                            foreach (var asset in (idsAndAssets ?? Enumerable.Empty<IdAndAssets>())
                                .SelectMany(idAndAssets => idAndAssets.Assets).Where(asset =>
                                    asset.Type == "video" && !string.IsNullOrWhiteSpace(asset.Url) &&
                                    Uri.IsWellFormedUriString(asset.Url, UriKind.Absolute)))
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                result.Add(
                                    new AerialVideo(
                                        asset.Id,
                                        new AerialVideoDownloadUrl(new Uri(asset.Url), AerialVideoQuality.SDR1080),
                                        asset.AccessibilityLabel,
                                        asset.TimeOfDay == "day"
                                            ? AerialVideoTimeOfDay.Day
                                            : (asset.TimeOfDay == "night"
                                                ? AerialVideoTimeOfDay.Night
                                                : AerialVideoTimeOfDay.Unspecified)));
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (DeserializationException)
                        {
                            // if it cannot be parsed, skip forward and try to parse it as versioned asset
                            goto VersionedAssets;
                        }
                    }

                    VersionedAssets:
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // assume it's a versioned list of assets, such as https://sylvan.apple.com/Aerials/2x/entries.json
                        var deserializedJsonAsDynamic = JSON.DeserializeDynamic(jsonString, desrializationOptions);
                        var versionInJson = deserializedJsonAsDynamic.version;
                        var assetsInJson = deserializedJsonAsDynamic.assets;

                        if (versionInJson != null && assetsInJson != null)
                        {
                            System.ComponentModel.TypeConverter versionTypeConverter =
                                System.ComponentModel.TypeDescriptor.GetConverter(versionInJson);
                            if (versionTypeConverter.CanConvertTo(typeof(int)) &&
                                versionTypeConverter.ConvertTo(versionInJson, typeof(int)) is int versionAsInt)
                            {
                                if (versionAsInt == 1)
                                {
                                    foreach (var asset in assetsInJson)
                                    {
                                        cancellationToken.ThrowIfCancellationRequested();

                                        System.ComponentModel.TypeConverter assetTypeConverter =
                                            System.ComponentModel.TypeDescriptor.GetConverter(asset);
                                        if (assetTypeConverter.CanConvertTo(typeof(IDictionary<string, dynamic>)) &&
                                            assetTypeConverter.ConvertTo(asset, typeof(IDictionary<string, dynamic>)) is
                                                IDictionary<string, dynamic> assetDictionary)
                                        {
                                            string id = assetDictionary.ContainsKey("id") ? assetDictionary["id"] : string.Empty;
                                            string accessibilityLabel = assetDictionary.ContainsKey("accessibilityLabel") ? assetDictionary["accessibilityLabel"] : string.Empty;
                                            string url1080SDR = assetDictionary.ContainsKey("url-1080-SDR") ? assetDictionary["url-1080-SDR"] : string.Empty;
                                            string url1080HDR = assetDictionary.ContainsKey("url-1080-HDR") ? assetDictionary["url-1080-HDR"] : string.Empty;
                                            string url4KSDR = assetDictionary.ContainsKey("url-4K-SDR") ? assetDictionary["url-4K-SDR"] : string.Empty;
                                            string url4KHDR = assetDictionary.ContainsKey("url-4K-HDR") ? assetDictionary["url-4K-HDR"] : string.Empty;

                                            var videoDownloadUrls = new List<AerialVideoDownloadUrl>();
                                            if (!string.IsNullOrWhiteSpace(url1080SDR) && Uri.IsWellFormedUriString(url1080SDR, UriKind.Absolute))
                                            {
                                                videoDownloadUrls.Add(new AerialVideoDownloadUrl(new Uri(url1080SDR), AerialVideoQuality.SDR1080));
                                            }

                                            if (!string.IsNullOrWhiteSpace(url1080HDR) && Uri.IsWellFormedUriString(url1080HDR, UriKind.Absolute))
                                            {
                                                videoDownloadUrls.Add(new AerialVideoDownloadUrl(new Uri(url1080HDR), AerialVideoQuality.HDR1080));
                                            }

                                            if (!string.IsNullOrWhiteSpace(url4KSDR) && Uri.IsWellFormedUriString(url4KSDR, UriKind.Absolute))
                                            {
                                                videoDownloadUrls.Add(new AerialVideoDownloadUrl(new Uri(url4KSDR), AerialVideoQuality.SDR4K));
                                            }

                                            if (!string.IsNullOrWhiteSpace(url4KHDR) && Uri.IsWellFormedUriString(url4KHDR, UriKind.Absolute))
                                            {
                                                videoDownloadUrls.Add(new AerialVideoDownloadUrl(new Uri(url4KHDR), AerialVideoQuality.HDR4K));
                                            }

                                            if (string.IsNullOrWhiteSpace(id) ||
                                                string.IsNullOrWhiteSpace(accessibilityLabel) ||
                                                videoDownloadUrls.Count == 0)
                                            {
                                                continue;
                                            }

                                            // else
                                            cancellationToken.ThrowIfCancellationRequested();

                                            result.Add(
                                                new AerialVideo(
                                                    id,
                                                    videoDownloadUrls,
                                                    accessibilityLabel,
                                                    AerialVideoTimeOfDay.Unspecified));
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                }
                                else
                                {
                                    goto UnknownJsonFormat;
                                }
                            }
                            else
                            {
                                goto UnknownJsonFormat;
                            }
                        }
                        else
                        {
                            goto UnknownJsonFormat;
                        }
                    }
                    UnknownJsonFormat:
                    {
                        // ToDo: well.. probably log
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            return result
                .OrderBy(aerialVideo => aerialVideo.ToString(), StringComparer.InvariantCultureIgnoreCase)
                .ToList();
        }

    }
}