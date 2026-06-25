---
version: alpha
name: "Monolegal Judicial Green"
description: "Monolegal is a Colombian legal-tech SaaS platform for litigating lawyers. The design uses a full-bleed dark video hero overlaid with high-contrast white and lime-green (#aed831) typography. Navigation is a white bar with dark text. The brand accent is a vivid yellow-green used on primary CTAs and headline emphasis. Typography is dominated by heavy Roboto weights (w700–w900) for headings and Helvetica Neue for body copy. The radius language is minimal (2px), and elevation is conveyed through subtle box shadows rather than heavy drop shadows."
extract-from: "https://www.design-extractor.com/"
colors:
  brand-lime-dark: "#82a025"
  hero-overlay: "#0a0a0a"
  surface-white: "#ffffff"
  brand-lime: "#aed831"
  link-blue: "#337ab7"
  muted-text: "#999999"
  text-dark: "#333333"
  border-subtle: "#9cbc46"
typography:
  hero-headline:
    fontFamily: "Roboto"
    fontSize: "75px"
    fontWeight: "700"
    lineHeight: "75px"
  section-heading-xl:
    fontFamily: "Roboto"
    fontSize: "48px"
    fontWeight: "500"
    lineHeight: "52.8px"
  heading-large:
    fontFamily: "Roboto"
    fontSize: "22px"
    fontWeight: "700"
    lineHeight: "35.2px"
    letterSpacing: "-0.22px"
  heading-medium:
    fontFamily: "Roboto"
    fontSize: "18px"
    fontWeight: "900"
    lineHeight: "28.8px"
    letterSpacing: "-0.18px"
  heading-medium-bold:
    fontFamily: "Roboto"
    fontSize: "18px"
    fontWeight: "700"
    lineHeight: "28.8px"
    letterSpacing: "-0.18px"
  display-brand:
    fontFamily: "Gotham Black"
    fontSize: "30px"
    fontWeight: "700"
    lineHeight: "33px"
    letterSpacing: "-0.18px"
  body-default:
    fontFamily: "Helvetica Neue"
    fontSize: "16px"
    fontWeight: "400"
    lineHeight: "22.8571px"
  body-large:
    fontFamily: "Helvetica Neue"
    fontSize: "22px"
    fontWeight: "400"
    lineHeight: "35.2px"
  button-label:
    fontFamily: "Roboto"
    fontSize: "16px"
    fontWeight: "700"
    lineHeight: "25.6px"
    letterSpacing: "-0.16px"
  caption-small:
    fontFamily: "Roboto"
    fontSize: "12px"
    fontWeight: "400"
    lineHeight: "19.2px"
  icon-font:
    fontFamily: "icomoon"
    fontSize: "48px"
    fontWeight: "400"
    lineHeight: "48px"
rounded:
  button: "2px"
  card: "7px"
spacing:
  xs: "4px"
  sm: "5px"
  md-sm: "8px"
  md: "10px"
  md-lg: "12px"
  base: "15px"
  lg: "16px"
  xl: "20px"
  2xl: "30px"
  3xl: "32px"
  4xl: "40px"
  5xl: "55px"
  6xl: "60px"
  7xl: "80px"
  sidebar: "270px"
---

## Overview

Monolegal is a Colombian legal-tech SaaS platform for litigating lawyers. The design uses a full-bleed dark video hero overlaid with high-contrast white and lime-green (#aed831) typography. Navigation is a white bar with dark text. The brand accent is a vivid yellow-green used on primary CTAs and headline emphasis. Typography is dominated by heavy Roboto weights (w700–w900) for headings and Helvetica Neue for body copy. The radius language is minimal (2px), and elevation is conveyed through subtle box shadows rather than heavy drop shadows.

**Signature traits:**
- Dual typeface system: Pairs Roboto and Gotham Black across the type hierarchy.
- Layered elevation: Depth comes from 5 validated shadow tokens.

## Colors

The palette uses 8 validated color tokens across 1 theme profile. Semantic roles stay attached to observed usage so generation agents can choose accents without inventing new color meaning.

**Semantic naming:**
- **action-text** maps to `brand-lime`: Role "text" is grounded by usage context "Primary CTA button fill (¡Pruébalo Gratis!), headline accent text ('Abogados Litigantes'), brand highlight".
- **action-background** maps to `brand-lime-dark`: Role "background" is grounded by usage context "Hover/pressed state for lime CTA buttons, secondary brand accent surfaces".
- **surface-background** maps to `surface-white`: Role "background" is grounded by usage context "Navigation bar background, card surfaces, hero overlay text color".
- **content-text** maps to `muted-text`: Role "text" is grounded by usage context "Secondary/muted text, captions, placeholder text".

### Text Scale
- **Brand Lime** (#aed831): Primary CTA button fill (¡Pruébalo Gratis!), headline accent text ('Abogados Litigantes'), brand highlight. Role: text. {authored: rgb(174, 216, 49), space: rgb}
- **Link Blue** (#337ab7): Hyperlinks, secondary interactive elements — Bootstrap-derived link color (206 hits). Role: text. {authored: rgb(51, 122, 183), space: rgb}
- **Muted Text** (#999999): Secondary/muted text, captions, placeholder text. Role: text. {authored: rgb(153, 153, 153), space: rgb}
- **Text Dark** (#333333): Primary body text, nav link text, footer text — most frequent text color (284 hits). Role: text. {authored: rgb(51, 51, 51), space: rgb}

### Interactive
- **Border Subtle** (#9cbc46): Lime-tinted borders on CTA button outlines and interactive element borders. Role: border. {authored: rgb(156, 188, 70), space: rgb}

### Surface & Shadows
- **Brand Lime Dark** (#82a025): Hover/pressed state for lime CTA buttons, secondary brand accent surfaces. Role: background. {authored: rgb(130, 160, 37), space: rgb}
- **Hero Overlay** (#0a0a0a): Dark semi-transparent overlay on video hero section. Role: background. {authored: rgba(10, 10, 10, 0.7), space: rgb, alpha: 0.7}
- **Surface White** (#ffffff): Navigation bar background, card surfaces, hero overlay text color. Role: background. {authored: rgb(255, 255, 255), space: rgb, alpha: 0.15}

## Typography

Typography uses Roboto, Gotham Black, Helvetica Neue, icomoon across extracted hierarchy roles. Keep hierarchy mapped to these token rows before adding decorative type styles.

Mixes Roboto and Gotham Black and Helvetica Neue and icomoon for visual contrast. Weight range spans bold, medium, regular. Sizes range from 12px to 75px.

### Font Roles
- **Headline Font**: Roboto
- **Body Font**: Roboto

### Type Scale Evidence
| Role | Font | Size | Weight | Line Height | Letter Spacing | Stack / Features | Notes |
|------|------|------|--------|-------------|----------------|------------------|-------|
| Main hero h1 title — largest display text on page | Roboto | 75px | 700 | 75px | normal | Roboto | Extracted token |
| Large section headings and feature titles | Roboto | 48px | 500 | 52.8px | normal | Roboto | Extracted token |
| Card headings, sub-section titles | Roboto | 22px | 700 | 35.2px | -0.22px | Roboto | Extracted token |
| Feature item headings, bold callout labels | Roboto | 18px | 900 | 28.8px | -0.18px | Roboto | Extracted token |
| Sub-headings, emphasized body titles | Roboto | 18px | 700 | 28.8px | -0.18px | Roboto | Extracted token |
| Brand display text, logo wordmark area | Gotham Black | 30px | 700 | 33px | -0.18px | Gotham Black | Extracted token |
| Primary body copy, paragraph text, nav items | Helvetica Neue | 16px | 400 | 22.8571px | normal | Helvetica Neue, Helvetica, Arial, sans-serif | Extracted token |
| Lead paragraph text, hero subtitle/description | Helvetica Neue | 22px | 400 | 35.2px | normal | Helvetica Neue, Helvetica, Arial, sans-serif | Extracted token |
| CTA button labels, nav action links | Roboto | 16px | 700 | 25.6px | -0.16px | Roboto | Extracted token |
| Fine print, footnotes, metadata labels | Roboto | 12px | 400 | 19.2px | normal | Roboto | Extracted token |
| Feature icons, UI iconography | icomoon | 48px | 400 | 48px | normal | icomoon | Extracted token |

## Layout

Responsive system uses 4 breakpoint tier(s): mobile, tablet, desktop, wide.

This system uses a 5px base grid with scale values 4, 5, 8, 10, 12, 15, 16, 20, 30, 32, 40, 55, 60, 80.

### Responsive Strategy
- **mobile (<= 1550px)**: Constrain layout for small viewports and prioritize vertical stacking.
- **tablet (768-1200px)**: Increase spacing and column structure for medium-width viewports.
- **desktop (>= 1200px)**: Expand layout density and horizontal composition for wide viewports.
- **wide (>= 1600px)**: Stretch composition with generous gutters and wider layout spans.

### Spacing System
| Token | Value | Px | Notes |
|------|-------|----|-------|
| xs | 4px | 4 | Extracted spacing token |
| sm | 5px | 5 | Extracted spacing token |
| md-sm | 8px | 8 | Extracted spacing token |
| md | 10px | 10 | Extracted spacing token |
| md-lg | 12px | 12 | Extracted spacing token |
| base | 15px | 15 | Extracted spacing token |
| lg | 16px | 16 | Extracted spacing token |
| xl | 20px | 20 | Extracted spacing token |
| 2xl | 30px | 30 | Extracted spacing token |
| 3xl | 32px | 32 | Extracted spacing token |
| 4xl | 40px | 40 | Extracted spacing token |
| 5xl | 55px | 55 | Extracted spacing token |
| 6xl | 60px | 60 | Extracted spacing token |
| 7xl | 80px | 80 | Extracted spacing token |
| sidebar | 270px | 270 | Extracted spacing token |

## Elevation & Depth

Keep depth flat unless validated shadow or interaction evidence appears in the extraction payload. Do not invent shadows beyond this evidence boundary.

### Shadow Evidence
| Shadow Token | Layers | Details |
|--------------|--------|---------|
| transparent-subtle | 1 | 1px 2px 4px 0px rgba(0, 0, 0, 0) |
| card-lift | 1 | 0px 5px 10px 0px rgba(0, 0, 0, 0.1) |
| hero-depth | 1 | 6px 11px 24px -7px rgba(0, 0, 0, 0.43) |
| nav-quick-link | 2 | 0px 1px 1px 0px rgba(0, 0, 0, 0.2) |
| sidebar-deep | 1 | -1px 0px 26px 6px rgba(0, 0, 0, 0.32) |

### Interaction Signals
| Theme | Signal | Evidence |
|-------|--------|----------|
| Light | outline-color | rgb(51, 51, 51) ; rgb(255, 255, 255) ; rgb(51, 122, 183) |
| Light | outline-width | 3px |
| Light | outline-offset | 0px |
| Light | transform | matrix(1, 0, 0, 1, 0, -20) ; matrix(1, 0, 0, 1, 0, 0) ; matrix(0.85, 0, 0, 0.85, 0, 0) |

## Shapes

Shape language maps directly to rounded tokens. Keep component corners consistent with the role mapping below before introducing bespoke geometry.

### Radius Roles
| Token | Value | Px | Role Mapping |
|------|-------|----|--------------|
| button | 2px | 2 | Hairline corner |
| card | 7px | 7 | Control corner |

### Geometry Evidence
| Radius Token | Shape | Units |
|--------------|-------|-------|
| button | 2px | px |
| card | 7px | px |

## Components

(none detected)

## Do's and Don'ts

Guardrails protect Dual typeface system, Layered elevation without adding unsupported visual claims.

| Do | Don't |
|----|---------|
| Do maintain consistent spacing using the base grid | Don't make unsupported claims about absent visual features |
| Do maintain WCAG AA contrast ratios (4.5:1 for normal text) | Don't mix rounded and sharp corners in the same view |
| Do use the primary color only for the single most important action per screen |  |
| Do verify evidence before writing new design-system guidance |  |

## Responsive Evidence

### Breakpoints
| Name | Width | Key Changes |
|------|-------|-------------|
| Mobile | <= 320px | (max-width: 320px) |
| Mobile | <= 360px | (max-width: 360px) |
| Mobile | <= 361px | (max-width: 361px) |
| Mobile | <= 375px | (max-width: 375px) |
| Mobile | <= 430px | (max-width: 430px) |
| Mobile | <= 480px | (max-width: 480px) |
| Mobile | <= 500px | only screen and (max-width: 500px) |
| Mobile | <= 720px | (max-width: 720px) |
| Mobile | <= 767px | (max-width: 767px) |
| Breakpoint 10 | <= 780px | (max-width: 780px) |
| Breakpoint 11 | <= 800px | screen and (max-width: 800px) and (orientation: landscape), screen and (max-height: 300px) |
| Breakpoint 12 | <= 900px | (max-width: 900px) |
| Breakpoint 13 | <= 1024px | (max-width: 1024px) |
| Breakpoint 14 | <= 1300px | (max-width: 1300px) |
| Breakpoint 15 | <= 1550px | (max-width: 1550px) |
| Tablet | 768-991px | (min-width: 768px) and (max-width: 991px) |
| Tablet | 768-1023px | (min-width: 768px) and (max-width: 1023px) |
| Tablet | 768-1200px | (min-width: 768px) and (max-width: 1200px) |
| Tablet | >= 768px | (min-width: 768px) |
| Tablet | 992-1199px | (min-width: 992px) and (max-width: 1199px) |

## Agent Prompt Guide

### Example Component Prompts
- Create button component using validated primary color role and spacing tokens.
- Create card component with mapped radius role and evidence-backed elevation.
- Create form input component using inferred typography hierarchy and border roles.

### Iteration Guide
1. Start with extracted palette and typography roles only.
2. Map spacing and radius directly from token tables before visual polish.
3. Apply component patterns one section at a time and compare against source intent.
4. Keep elevation claims tied to explicit evidence in output.
5. Iterate with smallest diffs and re-check section hierarchy after each change.
