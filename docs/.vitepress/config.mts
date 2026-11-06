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
    sidebar: [
      {
        text: "Start",
        items: [
          { text: "Overview", link: "/" },
          { text: "Getting started", link: "/getting-started" },
        ],
      },
      {
        text: "Architecture",
        items: [
          { text: "System architecture", link: "/architecture" },
          { text: "Database", link: "/database" },
          { text: "Concurrency", link: "/concurrency" },
          { text: "Design decisions", link: "/design-decisions" },
        ],
      },
      {
        text: "Features",
        items: [
          { text: "Authentication", link: "/features/authentication" },
          { text: "Matching", link: "/features/matching" },
          { text: "Map", link: "/features/map" },
          { text: "Attachments", link: "/features/attachments" },
          { text: "Audit logging", link: "/features/audit-log" },
        ],
      },
      {
        text: "Infrastructure",
        items: [{ text: "Deployment", link: "/deployment" }],
      },
      {
        text: "Project",
        items: [
          { text: "Testing", link: "/testing" },
          { text: "AI usage", link: "/ai-usage" },
        ],
      },
    ],

    socialLinks: [{ icon: "github", link: repoUrl }],

    footer: { message: "IS-202 programming project" },
  },
});
