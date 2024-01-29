import { defineConfig } from "astro/config";
import starlight from "@astrojs/starlight";

// https://astro.build/config
export default defineConfig({
  site: "https://bebop.sh",
  integrations: [
    starlight({
      title: "Bebop Docs",
      favicon: "./src/assets/favicon-32.png",
      expressiveCode: true,
      /* head: [
        {
          tag: "script",
          attrs: {
            src: "https://scripts.simpleanalyticscdn.com/latest.js",
            defer: true,
            "data-domain": "docs.bebop.sh",
          },
        },
      ],*/

      logo: {
        light: "./src/assets/logo.svg",
        dark: "./src/assets/logo.svg",
        replacesTitle: true,
      },

      social: {
        github: "https://github.com/betwixt-labs/bebop",
        discord: "https://discord.gg/jR3tfchVvW",
        twitter: "https://twitter.com/BetwixtLabs",
      },

      defaultLocale: "root",
      locales: {
        root: { label: "English", lang: "en" },
      },

      sidebar: [
        {
          label: "Guide",
          items: [
            { label: "Installation", link: "/guide/installation" },
            {
              label: "Playgrounds / REPL",
              link: "/guide/playground",
            },
            {
              label: "Getting Started (C#)",
              link: "/guide/getting-started-csharp",
            },
            {
              label: "Getting Started (Rust)",
              link: "/guide/getting-started-rust",
            },
            {
              label: "Getting Started (TypeScript)",
              link: "/guide/getting-started-typescript",
            },
          ],
        },
        {
          label: "Project Configuration",
          autogenerate: { directory: "configuration" },
        },
        {
          label: "Reference",
          autogenerate: { directory: "reference" },
        },
        {
          label: "Chords (Extensions)",
          autogenerate: { directory: "chords" },
        },
        {
          label: "Tempo (RPC)",
          autogenerate: { directory: "tempo" },
        },
        { label: "Known issues", link: "/known-issues" },
        { label: "FAQ", link: "/faq" },
      ],
    }),
  ],
});
