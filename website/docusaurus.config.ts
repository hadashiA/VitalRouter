import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'VitalRouter',
  tagline: 'Dinosaurs are cool',
  // favicon: 'img/favicon.ico',
  favicon: 'img/logo.svg',

  // Set the production url of your site here
  url: 'https://vitalrouter.hadashikick.jp',
  // Set the /<baseUrl>/ pathname under which your site is served
  // For GitHub pages deployment, it is often '/<projectName>/'
  baseUrl: '/',

  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: 'hadashiA',
  projectName: 'VitalRouter',

  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
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
          sidebarCollapsed: false,
          sidebarCollapsible: false,
          routeBasePath: '/',
          editUrl: 'https://github.com/hadashiA/VitalRouter/tree/main/website',
        },
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  // plugins: [
  //   [
  //     '@docusaurus/plugin-ideal-image',
  //     {
  //       quality: 70,
  //       max: 1030, // max resized image's size.
  //       min: 640, // min resized image's size. if original is lower, use that size.
  //       steps: 2, // the max number of images generated between min and max (inclusive)
  //       disableInDev: false,
  //     },
  //   ]
  // ],

  themeConfig: {
    // Replace with your project's social card
    // image: 'img/docusaurus-social-card.jpg',
    navbar: {
      // title: 'VitalRouter',
      logo: {
        alt: 'VitalRouter',
        src: 'img/logo2.svg',
      },
      items: [
        {
          type: 'localeDropdown',
          position: 'left',
        },
        // {
        //   to: 'getting-started/installation',
        //   activeBasePath: 'none',
        //   label: 'Getting Started',
        //   position: 'right',
        // },
        {
          href: 'https://github.com/hadashiA/VitalRouter',
          "aria-label": 'Github Repository',
          position: 'right',
          className: 'header-github-link'
        },
        {
          href: 'https://github.com/hadashiA/VitalRouter/releases',
          'label': 'v1.16.0',
          position: 'right',
        },
      ],
    },
    footer: {
      copyright: `Copyright © ${new Date().getFullYear()} <a href="https://twitter.com/hadashiA">hadashiA</a>`,
      // logo: {
      //   alt: 'VitalRouter',
      //   src: 'img/favicon.png',
      //   href: 'https://github.com/hadashiA/VitalRouter',
      // },
    },
    prism: {
      additionalLanguages: ['csharp', 'ruby'],
      theme: prismThemes.jettwaveDark,
      darkTheme: prismThemes.jettwaveDark,
    },
    colorMode: {
      defaultMode: 'light',
      disableSwitch: true,
      respectPrefersColorScheme: false,
    }
  } satisfies Preset.ThemeConfig,
};

export default config;
