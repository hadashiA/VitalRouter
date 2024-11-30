import type {SidebarsConfig} from '@docusaurus/plugin-content-docs';

const sidebars: SidebarsConfig = {
  docs: [
    {
      type: 'doc',
      id: 'intro',
      label: 'Intro',
    },
    {
      type: 'category',
      label: 'Getting Started',
      // link: { type: 'doc', id: 'getting-started/installation' },
      collapsed: false,
      collapsible: false,
      items: [
        'getting-started/installation',
        'getting-started/icommand',
        'getting-started/declarative-routing-pattern',
        'getting-started/event-handler-pattern',
      ]
    },
    {
      type: 'category',
      label: 'Dependency Injection (DI)',
      // link: { type: 'doc', id: 'di/intro' },
      collapsed: false,
      collapsible: false,
      items: [
        'di/intro',
        'di/vcontainer',
        'di/microsoft-extensions',
      ]
    },
    {
      type: 'category',
      label: 'Pipeline',
      // link: { type: 'doc', id: 'pipeline/interceptor' },
      collapsed: false,
      collapsible: false,
      items: [
        'pipeline/interceptor',
        'pipeline/sequential-control',
        'pipeline/publish-context',
        {
          type: 'category',
          label: 'Built-in interceptors',
          collapsed: false,
          collapsible: false,
          items: [
            'pipeline/command-pooling',
            'pipeline/fan-out',
          ]
        }
      ]
    },
    {
      type: 'category',
      label: 'Extensions',
      collapsed: false,
      collapsible: false,
      items: [
        'extensions/unitask',
        'extensions/r3',
        'extensions/mruby',
      ]
    }
  ]
};

export default sidebars;
