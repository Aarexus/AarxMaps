import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

const config: Config = {
  title: 'AarxMaps',
  tagline: 'Strongly typed .NET SDK for Google Maps Platform',
  favicon: 'img/favicon.ico',

  // Future flags, see https://docusaurus.io/docs/api/docusaurus-config#future
  // Every v4 flag except fasterByDefault, which needs the @docusaurus/faster package.
  future: {
    v4: {
      removeLegacyPostBuildHeadAttribute: true,
      useCssCascadeLayers: true,
      siteStorageNamespacing: true,
      fasterByDefault: false,
      mdx1CompatDisabledByDefault: true,
    },
  },

  url: 'https://aarexus.github.io',
  baseUrl: '/AarxMaps/',

  // GitHub Pages deployment config.
  organizationName: 'Aarexus',
  projectName: 'AarxMaps',

  onBrokenLinks: 'throw',
  markdown: {
    hooks: {
      onBrokenMarkdownLinks: 'throw',
    },
  },

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          editUrl: 'https://github.com/Aarexus/AarxMaps/tree/main/docs/',
        },
        blog: false,
        theme: { customCss: './src/css/custom.css' },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    image: 'img/social-card.png',
    colorMode: { respectPrefersColorScheme: true },
    navbar: {
      title: 'AarxMaps',
      logo: {
        alt: 'AarxMaps',
        src: 'img/aarxmaps-icon.png',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'tutorialSidebar',
          position: 'left',
          label: 'Docs',
        },
        {
          href: 'https://github.com/Aarexus/AarxMaps',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Packages',
          items: [
            { label: 'Core', to: '/docs/intro' },
            { label: 'Places', to: '/docs/places/overview' },
            { label: 'Routes', to: '/docs/routes/overview' },
          ],
        },
        {
          title: 'SDK Links',
          items: [
            { label: 'GitHub', href: 'https://github.com/Aarexus/AarxMaps' },
            { label: 'NuGet', href: 'https://www.nuget.org/packages?q=AarxMaps' },
          ],
        },
        {
          title: 'Personal Info',
          items: [
            { label: 'GitHub', href: 'https://github.com/JSebas-11' },
            { label: 'LinkedIn', href: 'https://www.linkedin.com/in/jsebas11/' },
            { label: 'Instagram', href: 'https://www.instagram.com/juans3_11.py/' },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} Aarexus Software. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['csharp'],
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
