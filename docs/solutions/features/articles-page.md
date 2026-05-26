---
date: 2026-04-03
title: "Articles Page Feature Design and Implementation"
tags: [articles, blazor, raindropio, masonry, azure-functions]
problem_type: feature-design
---

## Problem

The site had a Videos page displaying Raindrop.io content but no equivalent for articles. Users needed a dedicated interface to browse curated articles on programming, AI, .NET, C#, Blazor, and related technologies.

## Root Cause

Only the Videos content type was initially implemented from Raindrop.io. The Articles category (ID `56658122`) existed in Raindrop.io but had no Blazor page or Azure Function endpoint.

## Solution

Created a complete Articles page mirroring the Videos page patterns with article-specific optimizations:

### Architecture

- **Blazor page**: `Articles.razor` with code-behind at `Features/Pages/ArticlesPage/`
- **Azure Function**: `RaindropListArticles` at `/api/RaindropListArticles`
- **Route**: `/articles`
- **Data source**: Raindrop.io category ID `56658122`

### Key Design Decisions

- **No Raindrop.io login** — articles are publicly browsable without authentication (unlike Videos)
- **Manual fetch** via "Fetch Articles" button — no automatic loading
- **Masonry layout** optimized for article readability (larger text areas, no color changes from Videos)
- **Shimmer loading effects** for article thumbnails
- **Navigation item** placed after Videos with FontAwesome icon
- **Existing `RaindropItem` data structure** reused if compatible with Articles API response

### UI/UX Requirements

- Responsive masonry with 1-4 columns based on Foundation breakpoints
- Article cards display title, excerpt, creation date, and thumbnail
- Direct links open original articles in new tabs
- Clear loading states and actionable error messages
- WCAG 2.1 AA accessibility compliance

### Out of Scope

Search/filtering, bookmarking, commenting, content extraction, recommendation algorithms, and article editing.

## Prevention

- Verify API response structure compatibility before reusing data models
- Follow TDD for Azure Function development
- Maximize reuse of existing patterns and components
- Consider caching article data client-side for performance
