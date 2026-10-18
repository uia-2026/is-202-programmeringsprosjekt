import { defineConfig } from "vitepress";

// Replace both values before the first deploy
const repoName = "is-202-programmeringsprosjekt";
const repoUrl = "https://github.com/uia-2026/is-202-programmeringsprosjekt";

export default defineConfig({
  title: "Heimevernet",
  description: "Resource and need portal for crisis response",
  lang: "en-GB",

  // GitHub Pages serves a project site under /<repo-name>/
  base: `/${repoName}/`,
  cleanUrls: true,

  themeConfig: {
    nav: [
      { text: "Getting started", link: "/getting-started" },
      { text: "Architecture", link: "/architecture" },
      { text: "Testing", link: "/testing" },
    ],

    sidebar: [
      {
        text: "Start",
        items: [
          { text: "Overview", link: "/" },
          { text: "Getting started", link: "/getting-started" },
          { text: "Deployment", link: "/deployment" },
        ],
      },
      {
        text: "Design",
        items: [
          { text: "Architecture", link: "/architecture" },
          { text: "Design decisions", link: "/design-decisions" },
          { text: "Database", link: "/database" },
        ],
      },
      {
        text: "Features",
        items: [
          { text: "Authentication", link: "/features/authentication" },
          { text: "Matching", link: "/features/matching" },
          { text: "Map", link: "/features/map" },
          { text: "Attachments", link: "/features/attachments" },
        ],
      },
      {
        text: "Project",
        items: [
          { text: "Testing", link: "/testing" },
          { text: "AI usage", link: "/ai-usage" },
        ],
      },
    ],

    outline: [2, 3],
    search: { provider: "local" },

    editLink: {
      pattern: `${repoUrl}/edit/main/docs/:path`,
      text: "Edit this page on GitHub",
    },

    socialLinks: [{ icon: "github", link: repoUrl }],

    footer: { message: "IS-202 programming project" },
  },
});
