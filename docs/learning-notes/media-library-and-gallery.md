# Media Library and Gallery

Phase 9 separates image bytes from searchable SQL metadata. `MediaAsset` records original name, safe storage key, media facts, accessibility text, lifecycle state, and public URL. `GalleryAlbumMedia` is an explicit join so one image can be reused with album-specific order, caption, and alt text.

## Security Boundary

Only active Admin/SuperAdmin users can upload. The 10 MB configurable limit is checked before and after streaming. JPEG, PNG, and WebP must have matching extension, declared content type, codec format, valid dimensions, and a successful full decode. Original names are informational. Local keys are generated from date folders and GUIDs, path traversal is rejected, SVG and arbitrary files are unsupported, and API DTOs omit physical paths and storage keys.

## Request Flow

The browser posts multipart data to the same-origin Next.js handler. That server handler forwards it with the HttpOnly-session JWT. ASP.NET Core calls `MediaService`, `SkiaImageInspector`, and `IMediaStorage`, then saves metadata. Existing CMS URL fields remain intact; Media Picker simply fills them with an uploaded asset URL, preserving manual URL support.

Public album queries return only published albums and active images. Album overrides win over asset captions and alt text. `GET /media/{id}` returns active bytes with the recorded content type, range support, caching, and `nosniff`.

## Important Files

- `Application/Interfaces/IMediaStorage.cs`
- `Infrastructure/Media/LocalMediaStorage.cs`
- `Infrastructure/Media/SkiaImageInspector.cs`
- `Application/Services/MediaService.cs` and `GalleryService.cs`
- `components/admin/media-picker.tsx`
- Admin `/media` and `/gallery`; public `/gallery`

## Common Failures

- Missing tables: apply `AddMediaLibraryAndGallery`.
- Broken image URL: align `MediaStorage__PublicBaseUrl` with the running API profile.
- Rejected upload: check the 10 MB limit, extension/type agreement, and whether the image decodes.
- Delete conflict: remove CMS/gallery references or deactivate the asset.
- Stale public album: public pages revalidate on the configured cache interval.

Local uploads are development/demo data and are intentionally ignored by Git. Azure Blob Storage, transformations, cropping, video, documents, and rich text remain deferred.
