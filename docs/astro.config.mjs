import { defineConfig } from "astro/config";
import starlight from "@astrojs/starlight";

// https://astro.build/config
export default defineConfig({
  site: "https://docs.atuin.sh",
  integrations: [
    starlight({
      title: "Atuin Docs",
      favicon: "./src/assets/atuin.png",

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
        light: "./src/assets/logo-light.svg",
        dark: "./src/assets/logo-dark.png",
        replacesTitle: true,
      },

      social: {
        github: "https://github.com/atuinsh/atuin",
        discord: "https://discord.gg/jR3tfchVvW",
        mastodon: "https://hachyderm.io/@atuin",
        twitter: "https://twitter.com/atuinsh",
        linkedin: "https://www.linkedin.com/company/atuin/",
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
            { label: "Setting up sync", link: "/guide/sync" },
            { label: "Import existing history", link: "/guide/import" },
          ],
        },
        {
          label: "Configuration",
          autogenerate: { directory: "configuration" },
        },
        {
          label: "Reference",
          autogenerate: { directory: "reference" },
        },
        {
          label: "Self hosting",
          items: [
            { label: "Server setup", link: "/self-hosting/server-setup" },
            { label: "Usage", link: "/self-hosting/usage" },
            { label: "Docker", link: "/self-hosting/docker" },
            { label: "Kubernetes", link: "/self-hosting/kubernetes" },
          ],
        },
        { label: "Known issues", link: "/known-issues" },
        { label: "Integrations", link: "/integrations" },
        { label: "FAQ", link: "/faq" },
      ],
    }),
  ],
});
