import type { DashboardAnalytics } from '../api/types'
import {
  formatMinutes,
  formatMoney,
  formatNumber,
  profitLossTone,
} from '../utils/format'

interface DashboardSummaryProps {
  dashboard: DashboardAnalytics
}

interface MetricProps {
  label: string
  value: string
  note: string
  accent?: 'mint' | 'coral' | 'gold'
}

export function DashboardSummary({ dashboard }: DashboardSummaryProps) {
  return (
    <section className="section-stack" aria-labelledby="summary-title">
      <div className="section-heading">
        <div>
          <span className="eyebrow">Portfolio pulse</span>
          <h2 id="summary-title">Trading summary</h2>
        </div>
        <p>Completed fictional demo trades, calculated by the ASP.NET Core API.</p>
      </div>

      <div className="metric-grid">
        <Metric
          label="Total trades"
          value={formatNumber(dashboard.totalTrades, 0)}
          note={`${dashboard.breakEvenTrades} break-even`}
        />
        <Metric
          label="Profitable"
          value={formatNumber(dashboard.profitableTrades, 0)}
          note={`${formatNumber(dashboard.winRatePercentage, 2)}% win rate`}
          accent="mint"
        />
        <Metric
          label="Losing"
          value={formatNumber(dashboard.losingTrades, 0)}
          note="Completed positions"
          accent="coral"
        />
        <Metric
          label="Average duration"
          value={formatMinutes(dashboard.averageTradeDurationMinutes)}
          note={dashboard.mostActiveInstrument ?? 'No active instrument'}
          accent="gold"
        />
      </div>

      <div className="currency-grid" aria-label="Performance by currency">
        {dashboard.currencyPerformance.map((performance) => (
          <article className="currency-card" key={performance.currency}>
            <div className="currency-card__header">
              <div>
                <span className="eyebrow">{performance.currency} result</span>
                <strong
                  className={`money money--${profitLossTone(
                    performance.totalRealizedProfitLoss,
                  )}`}
                >
                  {formatMoney(
                    performance.totalRealizedProfitLoss,
                    performance.currency,
                  )}
                </strong>
              </div>
              <span className="currency-chip">{performance.currency}</span>
            </div>
            <div className="currency-card__extremes">
              <div>
                <span>Best</span>
                <strong>{performance.bestTrade.instrument}</strong>
                <small>
                  {formatMoney(
                    performance.bestTrade.realizedProfitLoss,
                    performance.currency,
                  )}
                </small>
              </div>
              <div>
                <span>Worst</span>
                <strong>{performance.worstTrade.instrument}</strong>
                <small>
                  {formatMoney(
                    performance.worstTrade.realizedProfitLoss,
                    performance.currency,
                  )}
                </small>
              </div>
            </div>
          </article>
        ))}
      </div>
    </section>
  )
}

function Metric({ label, value, note, accent }: MetricProps) {
  return (
    <article className={`metric-card${accent ? ` metric-card--${accent}` : ''}`}>
      <span>{label}</span>
      <strong>{value}</strong>
      <small>{note}</small>
    </article>
  )
}
