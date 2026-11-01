import type {ReactNode} from 'react';
import clsx from 'clsx';
import Link from '@docusaurus/Link';
import useBaseUrl from '@docusaurus/useBaseUrl';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Heading from '@theme/Heading';
import Layout from '@theme/Layout';

import styles from './index.module.css';

type Package = {
  name: string;
  to: string;
  description: string;
};

const PACKAGES: Package[] = [
  {
    name: 'Core',
    to: '/docs/core/configuration',
    description:
      'Registration, configuration, caching, errors and cancellation shared by every package, plus a client for any Google Maps endpoint.',
  },
  {
    name: 'Places',
    to: '/docs/places/overview',
    description:
      'Autocomplete, text and nearby search, place details, photos and reviews from the Places API (New).',
  },
  {
    name: 'Routes',
    to: '/docs/routes/overview',
    description:
      'Directions with legs, steps and transit details, and distance matrices from the Routes API.',
  },
];

function Hero(): ReactNode {
  const {siteConfig} = useDocusaurusContext();
  return (
    <header className={clsx('hero', styles.hero)}>
      <div className="container">
        <img
          className={styles.logo}
          src={useBaseUrl('/img/aarxmaps-icon.png')}
          alt=""
          width={96}
          height={96}
        />
        <Heading as="h1" className="hero__title">
          {siteConfig.title}
        </Heading>
        <p className={clsx('hero__subtitle', styles.tagline)}>{siteConfig.tagline}</p>
        <div className={styles.buttons}>
          <Link
            className={clsx('button button--primary button--lg', styles.primaryButton)}
            to="/docs/getting-started/quick-start">
            Get started
          </Link>
          <Link
            className={clsx('button button--lg', styles.secondaryButton)}
            href="https://github.com/Aarexus/AarxMaps">
            GitHub
          </Link>
        </div>
      </div>
    </header>
  );
}

function Packages(): ReactNode {
  return (
    <section className={styles.section}>
      <div className="container">
        <div className="row">
          {PACKAGES.map((pkg) => (
            <div key={pkg.name} className={clsx('col col--4', styles.cardColumn)}>
              <Link className={clsx('card', styles.card)} to={pkg.to}>
                <div className="card__body">
                  <Heading as="h3" className={styles.cardTitle}>
                    {pkg.name}
                  </Heading>
                  <p>{pkg.description}</p>
                </div>
              </Link>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

export default function Home(): ReactNode {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout title={siteConfig.title} description={siteConfig.tagline}>
      <Hero />
      <main>
        <Packages />
      </main>
    </Layout>
  );
}
