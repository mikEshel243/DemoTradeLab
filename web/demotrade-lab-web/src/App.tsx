import { useState } from 'react'
import type { TradeFilters as Filters } from './api/types'
import { DashboardSummary } from './components/DashboardSummary'
import { InstrumentSummaries } from './components/InstrumentSummaries'
import { ProfitLossTimeline } from './components/ProfitLossTimeline'
import { StatusPanel } from './components/StatusPanel'
import { TradeDetails } from './components/TradeDetails'
import { TradeFilters } from './components/TradeFilters'
import { TradeTable } from './components/TradeTable'
import { useOverview, useTradeDetails, useTrades } from './hooks/useApiData'

const defaultFilters: Filters = {
  instrument: '',
  currency: '',
  direction: '',
  outcome: '',
  sortBy: 'closedAtUtc',
  sortDirection: 'descending',
}

function App() {
  const [filters, setFilters] = useState<Filters>(defaultFilters)
  const [selectedTradeId, setSelectedTradeId] = useState<string | null>(null)
  const overview = useOverview()
  const trades = useTrades(filters)
  const tradeDetails = useTradeDetails(selectedTradeId)

  const refreshDashboard = () => {
    overview.reload()
    trades.reload()
  }

  return (
    <div className="app-shell">
      <header className="site-header">
        <a className="brand" href="#top" aria-label="DemoTradeLab home">
          <span className="brand__mark" aria-hidden="true">
            <i />
          </span>
          <span>
            <strong>DemoTradeLab</strong>
            <small>Local analytics workspace</small>
          </span>
        </a>
        <div className="header-actions">
          <span className="api-status">
            <i aria-hidden="true" /> API-backed
          </span>
          <button className="button button--secondary" type="button" onClick={refreshDashboard}>
            Refresh data
          </button>
        </div>
      </header>

      <main id="top">
        <section className="hero-section">
          <div className="hero-section__copy">
            <span className="eyebrow">Educational demo analytics</span>
            <h1>See the story behind every completed trade.</h1>
            <p>
              Explore fictional results, execution timing, and instrument patterns through a
              typed React client backed by ASP.NET Core.
            </p>
          </div>
          <div className="hero-section__signal" aria-hidden="true">
            <span>Realized performance</span>
            <svg viewBox="0 0 320 120">
              <path d="M4 96 42 82 74 89 111 58 145 65 184 38 222 49 268 18 316 27" />
              <circle cx="316" cy="27" r="6" />
            </svg>
            <small>Fictional data only</small>
          </div>
        </section>

        {overview.isLoading && !overview.data ? (
          <StatusPanel
            eyebrow="Connecting"
            title="Loading dashboard analytics"
            message="The frontend is requesting the summary, instrument groups, and timeline from the API."
          />
        ) : null}

        {overview.error ? (
          <StatusPanel
            eyebrow="Connection problem"
            title="The analytics API is unavailable"
            message={`${overview.error} Confirm that the ASP.NET Core API is running on port 5122.`}
            tone="error"
            actionLabel="Try again"
            onAction={overview.reload}
          />
        ) : null}

        {overview.data ? (
          <>
            <DashboardSummary dashboard={overview.data.dashboard} />

            {overview.data.dashboard.totalTrades === 0 ? (
              <StatusPanel
                eyebrow="Empty dataset"
                title="No completed trades yet"
                message="Create a fictional manual trade through the API or apply the sample-data migration to populate this dashboard."
              />
            ) : (
              <div className="analytics-grid">
                <ProfitLossTimeline timelines={overview.data.timelines} />
                <InstrumentSummaries summaries={overview.data.instruments} />
              </div>
            )}
          </>
        ) : null}

        <section className="trades-section" aria-labelledby="trades-title">
          <div className="section-heading">
            <div>
              <span className="eyebrow">Trade journal</span>
              <h2 id="trades-title">Completed trades</h2>
            </div>
            <p>Filters are sent to the backend; selecting a row calls the details endpoint.</p>
          </div>

          <TradeFilters
            filters={filters}
            instruments={overview.data?.instruments ?? []}
            onChange={setFilters}
            onReset={() => setFilters({ ...defaultFilters })}
          />

          {trades.error ? (
            <StatusPanel
              eyebrow="Request failed"
              title="Trades could not be loaded"
              message={trades.error}
              tone="error"
              actionLabel="Try again"
              onAction={trades.reload}
            />
          ) : null}

          <div className="trade-workspace">
            <div className="panel trade-panel">
              <div className="panel__header">
                <div>
                  <span className="eyebrow">Results</span>
                  <h3>
                    {trades.isLoading
                      ? 'Updating trades…'
                      : `${trades.data?.length ?? 0} matching trades`}
                  </h3>
                </div>
                {trades.isLoading ? <span className="loading-dot" aria-label="Loading" /> : null}
              </div>

              {trades.data && trades.data.length > 0 ? (
                <TradeTable
                  trades={trades.data}
                  selectedTradeId={selectedTradeId}
                  onSelect={setSelectedTradeId}
                />
              ) : null}

              {!trades.isLoading && !trades.error && trades.data?.length === 0 ? (
                <div className="inline-empty">
                  <strong>No trades match these filters.</strong>
                  <span>Reset the filters to return to the full journal.</span>
                </div>
              ) : null}

              {trades.isLoading && !trades.data ? (
                <div className="table-skeleton" aria-hidden="true">
                  <i />
                  <i />
                  <i />
                  <i />
                </div>
              ) : null}
            </div>

            <TradeDetails
              trade={tradeDetails}
              selectedTradeId={selectedTradeId}
              onClose={() => setSelectedTradeId(null)}
            />
          </div>
        </section>
      </main>

      <footer>
        <p>
          Unofficial educational project. Not affiliated with or connected to any financial
          institution.
        </p>
        <span>Fictional and manually entered data only.</span>
      </footer>
    </div>
  )
}

export default App
