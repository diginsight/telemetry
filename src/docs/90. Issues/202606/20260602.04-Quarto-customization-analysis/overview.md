---
title: "Quarto Styling & Theming — Implementation Analysis"
author: "Diginsight Telemetry"
date: "2026-06-03"
categories: [quarto, theming, css, documentation]
---

# Quarto Styling & Theming — Implementation Analysis

This document analyses **how the Diginsight Telemetry documentation site applies its
styles**, with particular focus on:

1. How the **default (light) styles** are applied.
2. How the **dark styles** are handled and how **contrast** is adjusted.
3. The **runtime theme switcher** that layers on top of the two static themes.

It then describes, step by step, **how the same approach can be reproduced in any
other Quarto repository**.

---

## 1. The four layers of styling

The site composes its look from **four cooperating layers**. Understanding the
ordering and precedence of these layers is the key to the whole design.

| # | Layer | File(s) | Loaded | Scope |
|---|-------|---------|--------|-------|
| 1 | **Bootswatch base theme** (`cosmo`) | bundled by Quarto | compiled into `quarto-*.css` | global Bootstrap variables |
| 2 | **Theme SCSS overrides** | `theme-light.scss`, `theme-dark.scss` | compiled into the light/dark bundle | navbar, banner, link colors |
| 3 | **Custom CSS** | `styles.css`, `styles-callouts.css` | linked at end of `<body>` | content, sidebar, highlights, callouts |
| 4 | **Runtime switcher** | `header-includes.html` | inline `<style>` + `<script>` in `<head>` | live Bootswatch swap + dark re-styling |

The cascade order is deliberate. Bootstrap variables come first (lowest
specificity), the SCSS bundle refines them, the custom CSS files (loaded *last* in
the body) win on equal specificity, and the runtime switcher injects an even-later
`<style>` element that can override everything when a user picks a non-default
theme.

```text
cosmo (Bootswatch)  ──►  theme-{light|dark}.scss  ──►  styles*.css  ──►  runtime <style> overrides
        defaults                SCSS rules              body-end link            head injection
```

---

## 2. How the default (light) styles are applied

### 2.1 Theme registration in `_quarto.yml`

The light/dark pair and the custom CSS are declared in the `format.html` block:

```yaml
format:
  html:
    theme:
      light: [cosmo, theme-light.scss]
      dark:  [cosmo, theme-dark.scss]
    css: [styles.css, styles-callouts.css]
    include-in-header: header-includes.html
    toc: true
```

- `theme.light` / `theme.dark` are **arrays**: the first entry is the Bootswatch
  base (`cosmo`), the rest are SCSS files layered on top. Quarto compiles each
  array into a single CSS bundle and emits a navbar **light/dark toggle** because
  both keys are present.
- `css:` adds plain CSS files that are linked **after** the compiled theme, so they
  win ties in the cascade.
- `include-in-header:` injects raw HTML into every page `<head>` — this is the hook
  for the runtime switcher.

### 2.2 `theme-light.scss` — SCSS variable + rule overrides

Quarto SCSS files use two labelled regions:

```scss
/*-- scss:defaults --*/   // Bootstrap/SASS variables — must come BEFORE Bootstrap
$navbar-bg: #c78e3c;       // custom amber navbar
$navbar-fg: white;
$navbar-hl: white;

/*-- scss:rules --*/      // plain CSS rules — emitted AFTER Bootstrap
.navbar-nav .nav-link:hover,
.navbar-nav .nav-link.active { color: white !important; }

.alert-primary {           // the announcement banner
  background-color: #fff9c477 !important;
  color: #333 !important;
}
```

Key points:

- **`scss:defaults`** sets SASS variables that Bootstrap reads while compiling.
  This is how the navbar gets its custom amber color without touching Bootstrap
  source.
- **`scss:rules`** is appended after Bootstrap, used here for hover states and the
  announcement banner (a light translucent yellow on dark text).

### 2.3 `styles.css` — content-level customizations

`styles.css` (linked at body-end) carries the things SCSS variables can't express:

- **Brand bulb icon** prepended to the navbar title via `.navbar-brand::before`.
- **Highlight color** for `<mark>` text — overrides Bootstrap's
  `--bs-highlight-bg` to a soft yellow `#fff7d1`.
- **H1 de-duplication** — hides the redundant in-content `<h1>` when Quarto already
  renders a title block, but restores it when there is no metadata block.
- **Bullet-list visibility fixes** for content lists.

### 2.4 `styles-callouts.css` — emoji-driven callouts

This file turns ordinary blockquotes into colored callouts based on a **leading
emoji**, e.g. a blockquote starting with `💡` becomes a green "tip" box:

```css
blockquote:has(strong:first-child:contains("💡")) {
  border-left-color: #198754;
  background-image: linear-gradient(135deg, #d1e7dd 0%, #f8f9fa 100%);
}
```

No special Quarto syntax is required — authors just write a blockquote whose first
bold run starts with the emoji.

---

## 3. How dark styles are handled & contrast is adjusted

Dark mode is handled in **three coordinated places**, because Quarto's native dark
toggle, the custom content CSS, and the runtime switcher each control a different
slice of the page.

### 3.1 The crucial gotcha — making the dark theme *actually* dark

Quarto decides whether a compiled theme is tagged `data-mode="light"` or
`"dark"` **from the brightness of the compiled `$body-bg`**. If `theme-dark.scss`
only changes the navbar color and leaves the background light, Quarto tags the
"dark" sheet as light, the navbar toggle never adds `body.quarto-dark`, and **no
dark styling ever applies**.

The fix (documented inline in `theme-dark.scss`) is to set genuinely dark surface
and text defaults:

```scss
/*-- scss:defaults --*/
$body-bg:    #1a1d20;   // dark surface — this is what makes Quarto tag it "dark"
$body-color: #dee2e6;   // light text

$link-color: #6ea8fe;   // brighter links for contrast on dark
$light:      #2b3035;   // card / sidebar / code-block surface
$border-color: #495057;
$code-color: #e685b5;   // inline code, lifted for legibility
$navbar-bg:  #8b6129;   // darker amber than the light theme's #c78e3c
```

**Contrast strategy:** every color is re-chosen for a dark background — text moves
to light grey, links to a brighter blue, the navbar to a *darker* amber, inline
code to a lifted pink. The announcement banner is also re-skinned:

```scss
.alert-primary {
  background-color: #4a4020 !important;   // muted dark amber (vs. light yellow)
  color: #f8f9fa !important;
}
```

### 3.2 Content-level dark rules in `styles.css`

When `body.quarto-dark` *is* present, `styles.css` adjusts the pieces that the SCSS
bundle doesn't reach:

```css
/* Highlighted text: a low-alpha yellow over dark just looks grey, so use a
   dark amber background with warm light text for a real dark-mode highlight. */
body.quarto-dark {
  --bs-highlight-bg: #4d3f17;
}
body.quarto-dark mark,
body.quarto-dark .mark {
  background-color: #4d3f17 !important;
  color: #ffe9a6 !important;
}

/* Docked left sidebar — force dark surface + readable item text. */
body.quarto-dark #quarto-sidebar {
  background-color: #2b3035 !important;
  border-color: #495057 !important;
}
body.quarto-dark #quarto-sidebar .sidebar-item a { color: #dee2e6 !important; }
body.quarto-dark #quarto-sidebar .sidebar-item a:hover,
body.quarto-dark #quarto-sidebar .sidebar-item a.active { color: #fff !important; }
```

The recurring **contrast pattern**: pick a dark surface (`#2b3035`), readable body
text (`#dee2e6`), and a pure-white hover/active state so the *interactive* element
visibly separates from the resting state.

### 3.3 Callout dark variants in `styles-callouts.css`

Callouts are re-derived for dark mode using the `[data-bs-theme="dark"]` selector
(set by Bootstrap 5.3 on the dark scope). Each callout keeps its accent border but
swaps to a **deep, desaturated fill** so colored text stays legible:

```css
[data-bs-theme="dark"] blockquote { background-color: #2b3035; color: #f8f9fa; }

[data-bs-theme="dark"] blockquote:has(strong:first-child:contains("💡")) {
  border-left-color: #198754;                 /* same accent */
  background-image: linear-gradient(135deg, #1a4d3a 0%, #2b3035 100%); /* deep green */
}
```

So the **contrast technique is consistent**: keep the semantic accent color, but
flip the fill from a *pale tint* (light mode) to a *deep shade* (dark mode) of the
same hue, always over the dark surface base.

### 3.4 Two dark selectors, two purposes

| Selector | Set by | Used for |
|----------|--------|----------|
| `body.quarto-dark` | Quarto's navbar toggle | sidebar, highlights (page chrome) |
| `[data-bs-theme="dark"]` | Bootstrap 5.3 scope | callouts (component-level) |

Both must be handled because Quarto and Bootstrap signal "dark" through different
attributes.

---

## 4. The runtime theme switcher (optional advanced layer)

`header-includes.html` adds a **client-side Bootswatch switcher** (25 themes) under
the navbar **About** menu, persisting the choice in `localStorage`. This is layered
*on top* of the two static themes and is independent of Quarto's own toggle.

### 4.1 What it does

- Maintains a `THEMES` table (name, mode, primary, bg, fg, border, font, and a
  `pair` = its opposite-mode sibling).
- On selection it injects a `<link>` to `https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/<name>/bootstrap.min.css`.
- **Neutralises the Diginsight customizations** so the chosen Bootswatch theme
  renders pristine: it disables `styles.css` / `styles-callouts.css`
  (`link.disabled = true; link.media = "not all"`) and injects an override
  `<style>` that resets the SCSS-baked amber navbar / yellow banner to the picked
  theme's own colors.
- **Re-applies dark styling for dark Bootswatch themes.** Because disabling
  `styles.css` removes the `body.quarto-dark` sidebar/highlight rules, the switcher
  re-emits equivalent rules using *the selected theme's own* `bg/fg/border` values
  — keeping the same contrast strategy described in §3.2.
- **Fixes dropdown contrast.** Quarto's navbar carries `data-bs-theme="dark"`; in
  Bootstrap 5.3 the dropdown menus inherit it and turn black under a light theme.
  `applyMenuTheme()` scopes each navbar dropdown to the selected theme's own mode.
- **Syncs with the native toggle.** A `MutationObserver` watches `body`'s class; if
  the user hits Quarto's native dark toggle while a custom theme is active, it swaps
  the custom theme to its opposite-mode `pair`.
- Applies the saved theme **in the `<head>` before paint** to minimise FOUC, then
  re-applies in `whenReady()` once the body-end CSS links exist.

### 4.2 Companion preview generator

`build-theme-previews.ps1` renders the site **once** and produces a comparison page
where each card deep-links the single render via `?theme=<name>` (read by the
switcher). This avoids rendering 25 near-identical ~16 MB copies (~390 MB) — the
only per-theme difference is the CDN-loaded Bootstrap CSS.

### 4.3 Bonus: resizable sidebar

The same file ships a **draggable left-sidebar resizer**: it re-flows the docked
`.page-columns` grid through a `--ds-sidebar-width` CSS variable, persists the width
in `localStorage`, and adds a drag handle (double-click to reset).

---

## 5. Reproducing this in any other repository

### 5.1 Minimum setup — static light + dark with custom contrast

This gives you the **default + dark styling with adjusted contrast** (sections 2–3)
without the runtime switcher.

1. **Add the theme files** at the project root:

   - `theme-light.scss`

     ```scss
     /*-- scss:defaults --*/
     $navbar-bg: #c78e3c;
     $navbar-fg: white;

     /*-- scss:rules --*/
     .alert-primary { background-color: #fff9c477 !important; color: #333 !important; }
     ```

   - `theme-dark.scss` — **must** set a dark `$body-bg` (see §3.1):

     ```scss
     /*-- scss:defaults --*/
     $body-bg:    #1a1d20;   // REQUIRED so Quarto tags this sheet "dark"
     $body-color: #dee2e6;
     $link-color: #6ea8fe;
     $light:      #2b3035;
     $border-color: #495057;
     $navbar-bg:  #8b6129;

     /*-- scss:rules --*/
     .alert-primary { background-color: #4a4020 !important; color: #f8f9fa !important; }
     ```

2. **Add `styles.css`** with the dual-selector dark rules:

   ```css
   :root { --bs-highlight-bg: #fff7d1; }
   mark, .mark { background-color: #fff7d1 !important; }

   body.quarto-dark { --bs-highlight-bg: #4d3f17; }
   body.quarto-dark mark { background-color: #4d3f17 !important; color: #ffe9a6 !important; }
   body.quarto-dark #quarto-sidebar { background-color: #2b3035 !important; }
   body.quarto-dark #quarto-sidebar .sidebar-item a { color: #dee2e6 !important; }
   ```

3. **Wire it up in `_quarto.yml`:**

   ```yaml
   format:
     html:
       theme:
         light: [cosmo, theme-light.scss]
         dark:  [cosmo, theme-dark.scss]
       css: [styles.css]
   ```

That's the complete portable core: **a light/dark Bootswatch pair, SCSS variable
overrides per mode, and a custom CSS file that re-skins `<mark>`, the sidebar, and
callouts for each mode.**

### 5.2 Add emoji callouts (optional)

Copy `styles-callouts.css`, add it to the `css:` array, and use both selectors
(`blockquote:has(...)` for light, `[data-bs-theme="dark"] blockquote:has(...)` for
dark) following the *pale tint → deep shade* contrast rule from §3.3.

### 5.3 Add the runtime switcher (optional, advanced)

1. Copy `header-includes.html` and reference it with
   `include-in-header: header-includes.html`.
2. Adjust the `THEMES` table colors if your base theme isn't `cosmo`, and update the
   `CUSTOM_CSS_RE` regex if your custom CSS files have different names.
3. (Optional) Copy `build-theme-previews.ps1` for the single-render comparison page.

### 5.4 Portability checklist

| Item | Required? | Notes |
|------|-----------|-------|
| `theme.light` / `theme.dark` arrays | yes | enables the navbar toggle |
| Dark `$body-bg` in dark SCSS | **yes** | otherwise dark mode never activates (§3.1) |
| `styles.css` with `body.quarto-dark` rules | recommended | sidebar + highlight contrast |
| `[data-bs-theme="dark"]` rules | for callouts | component-level dark scope |
| `header-includes.html` switcher | optional | adds 25-theme runtime picker |
| `build-theme-previews.ps1` | optional | comparison preview generator |

---

## 6. Key takeaways

- **Layered cascade**: Bootswatch → per-mode SCSS → body-end custom CSS → optional
  runtime overrides. Later layers win on ties; this ordering is intentional.
- **The dark-mode activation trap**: Quarto infers dark/light from `$body-bg`
  brightness. A dark theme *must* set a dark background or the toggle silently does
  nothing.
- **Two dark selectors**: `body.quarto-dark` (Quarto chrome) and
  `[data-bs-theme="dark"]` (Bootstrap components) — handle both.
- **Consistent contrast rule**: keep semantic accent hues, flip fills from pale
  tints (light) to deep shades (dark) over a fixed dark surface, and use pure white
  for hover/active so interactive elements separate from resting state.
- **Everything is portable**: the entire system is a handful of root-level files
  (`theme-*.scss`, `styles*.css`, optional `header-includes.html`) referenced from
  `_quarto.yml` — drop them into any Quarto website project.
