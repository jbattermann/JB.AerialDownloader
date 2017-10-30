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

namespace JB.AerialDownloader.Commands
{
    public class DownloadAerialMoviesCommand : ICommand<DownloadAerialMoviesOptions>
    {
        /// <summary>
        /// Executes the command and returns an exit code.
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

        private async Task<ICollection<AerialVideo>> GetVideosFromJsonUrl(string jsonUrl, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = new List<AerialVideo>();

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
                            idsAndAssets = Jil.JSON.Deserialize<IdAndAssets[]>(jsonString,
                                Jil.Options.ExcludeNullsIncludeInheritedCamelCase);

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
                        catch (Exception)
                        {
                            // if it cannot be parsed, skip forward and try to parse it as versioned asset
                            goto VersionedAssets;
                        }
                    }
                    
                    VersionedAssets:
                    {
                        //// assume it's a versioned list of assets, such as https://sylvan.apple.com/Aerials/2x/entries.json
                        //if (NetJSON.NetJSON.DeserializeObject(jsonString) is Dictionary<string, object> parsedObject)
                        //{
                        //    var normalizedDictionary = new Dictionary<string, object>(parsedObject, StringComparer.InvariantCultureIgnoreCase);
                        //    // this is a version 1 assets list
                        //    if (normalizedDictionary.ContainsKey("version") && !string.IsNullOrWhiteSpace(normalizedDictionary["version"]?.ToString()) && normalizedDictionary["version"].ToString().Trim() == "1")
                        //    {
                        //        if (normalizedDictionary.ContainsKey("assets") &&
                        //            normalizedDictionary["assets"] is List<object> assetsAsObjects)
                        //        {
                        //            foreach (IDictionary<string, object> assetAsDictionary in assetsAsObjects)
                        //            {
                        //                cancellationToken.ThrowIfCancellationRequested();
                        //            }
                        //        }
                        //        else
                        //        {
                        //            // ToDo: uhm - this is unexpected - log maybe
                        //        }
                        //    }
                        //    else
                        //    {
                        //        // ToDo: That's a new one.. which we don't handle, yet!? Probably log
                        //    }
                        //}
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