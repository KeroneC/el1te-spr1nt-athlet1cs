# ADR 011: Use a Storage Abstraction for Media

## Status

Accepted in Phase 9.

## Decision

Store image metadata and relationships in SQL, but store bytes through application interface `IMediaStorage`. Development uses `LocalMediaStorage`; a future deployment phase may provide Azure Blob Storage without changing controllers or domain entities.

## Rationale

Database binaries would enlarge backups, couple content delivery to SQL, and make caching harder. Generated storage keys prevent user filenames from becoming paths. Public URLs identify active assets without exposing keys or physical locations. Deletion first checks CMS and gallery references.

Image inspection uses maintained MIT-licensed SkiaSharp 3.119.2 because format signatures, dimensions, and full decoding should use a proven codec rather than hand-written byte checks.

## Tradeoffs

Local files are simple for development but are not durable across production instance replacement or scale-out. Metadata and file deletion cannot share one transaction, so failed cleanup is logged. Public URLs currently include the configured API origin and must be set correctly per environment.

## Future

Implement an Azure Blob `IMediaStorage` provider in a separate deployment phase. No Azure resource or Blob package is part of this decision.
