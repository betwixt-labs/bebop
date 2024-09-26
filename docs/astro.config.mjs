import { defineConfig } from "astro/config";
import starlight from "@astrojs/starlight";

const bebop = JSON.parse(fs.readFileSync('../vscode-bebop/syntaxes/bebop.tmLanguage.json', 'utf8'));

// https://astro.build/config
export default defineConfig({
  site: "https://bebop.sh",
  integrations: [
    starlight({
      title: "Bebop Docs",
      favicon: "./src/assets/favicon-32.png",
      expressiveCode: true,

      logo: {
        light: "./src/assets/logo.svg",
        dark: "./src/assets/logo.svg",
        replacesTitle: true,
      },

      social: {
        github: "https://github.com/betwixt-labs/bebop",
        discord: "https://discord.gg/jVfz9sMPWv",
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
          items: [
            {
              label: "What are Chords?",
              link: "/chords/what-are-chords",
            },
            { label: "Installing Extensions", link: "/chords/installing" },
            {
              label: "Guides", items: [
                {
                  label: "Authoring Extensions",
                  link: "/chords/guides/authoring-extensions",
                },
                {
                  label: "Publishing Extensions",
                  link: "/chords/guides/publishing-extensions",
                }
              ]
            },
            {
              label: "References", items: [
                {
                  label: "chord.json",
                  link: "/chords/chord-json",
                },
                {
                  label: "chordc",
                  link: "/chords/chordc",
                },
              ]
            }
          ],
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
  markdown: {
    shikiConfig: {
      langs: [
        {
          id: 'bebop',
          scopeName: 'source.bebop',
          grammar: bebop,
          aliases: ['bop'],
        },
      ],
    },
  },
});
