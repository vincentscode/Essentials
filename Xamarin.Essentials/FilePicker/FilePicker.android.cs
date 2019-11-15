﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Provider;

namespace Xamarin.Essentials
{
    public static partial class FilePicker
    {
        static async Task<FilePickerResult> PlatformPickFileAsync(PickOptions options)
        {
            // we only need the permission when accessing the file, but it's more natural
            // to ask the user first, then show the picker.
            await Permissions.RequireAsync(PermissionType.ReadExternalStorage);

            var intent = new Intent(Intent.ActionGetContent);
            intent.SetType("*/*");
            intent.AddCategory(Intent.CategoryOpenable);

            var allowedTypes = options?.FileTypes?.Value?.ToArray();
            if (allowedTypes?.Length > 0)
                intent.PutExtra(Intent.ExtraMimeTypes, allowedTypes);

            var pickerIntent = Intent.CreateChooser(intent, options.PickerTitle ?? "Select file");

            try
            {
                var result = await IntermediateActivity.StartAsync(pickerIntent, 12345);
                var contentUri = result.Data;
                return new FilePickerResult(contentUri);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }
    }

    public partial class FilePickerFileType
    {
        public static FilePickerFileType PlatformImageFileType() =>
            new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "image/png", "image/jpeg" } }
            });

        public static FilePickerFileType PlatformPngFileType() =>
            new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "image/png" } }
            });
    }

    public partial class PickerResultBase
    {
        readonly global::Android.Net.Uri contentUri;
        readonly string fullPath;

        internal PickerResultBase(global::Android.Net.Uri contentUri)
        {
            this.contentUri = contentUri;
            fullPath = GetFullPath(contentUri);
            FileName = GetFileName(contentUri);
        }

        static string GetFullPath(global::Android.Net.Uri contentUri)
        {
            // if this is a file, use that
            if (contentUri.Scheme == "file")
                return contentUri.Path;

            // ask the content provider for the data column, which may contain the actual file path
            var path = QueryContentResolverColumn(contentUri, MediaStore.Files.FileColumns.Data);
            if (!string.IsNullOrEmpty(path) && Path.IsPathRooted(path))
                return path;

            // fallback: use content URI
            return contentUri.ToString();
        }

        static string GetFileName(global::Android.Net.Uri contentUri)
        {
            // resolve file name by querying content provider for display name
            var filename = QueryContentResolverColumn(contentUri, MediaStore.MediaColumns.DisplayName);

            if (!string.IsNullOrWhiteSpace(filename))
                return filename;

            return Path.GetFileName(WebUtility.UrlDecode(contentUri.ToString()));
        }

        static string QueryContentResolverColumn(global::Android.Net.Uri contentUri, string columnName)
        {
            string text = null;

            var projection = new[] { columnName };
            using var cursor = Application.Context.ContentResolver.Query(contentUri, projection, null, null, null);
            if (cursor?.MoveToFirst() == true)
            {
                var columnIndex = cursor.GetColumnIndex(columnName);
                if (columnIndex != -1)
                    text = cursor.GetString(columnIndex);
            }

            return text;
        }

        Task<Stream> PlatformOpenReadStreamAsync()
        {
            if (contentUri.Scheme == "content")
            {
                var content = Application.Context.ContentResolver.OpenInputStream(contentUri);
                return Task.FromResult(content);
            }

            var stream = File.OpenRead(fullPath);
            return Task.FromResult<Stream>(stream);
        }
    }

    public partial class FilePickerResult
    {
        internal FilePickerResult(global::Android.Net.Uri contentUri)
            : base(contentUri)
        {
        }
    }
}