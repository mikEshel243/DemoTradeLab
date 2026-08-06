import type { InstrumentSummary } from '../api/types'
import { formatMinutes, formatMoney, profitLossTone } from '../utils/format'

interface InstrumentSummariesProps {
  summaries: InstrumentSummary[]
}

export function InstrumentSummaries({ summaries }: InstrumentSummariesProps) {
  return (
    <section className="panel" aria-labelledby="instruments-title">
      <div className="panel__header">
        <div>
          <span className="eyebrow">Concentration</span>
          <h2 id="instruments-title">By instrument</h2>
        </div>
        <span className="panel__count">{summaries.length} groups</span>
      </div>

      <div className="instrument-list">
        {summaries.map((summary) => (
          <article
            className="instrument-row"
            key={`${summary.instrument}-${summary.currency}`}
          >
            <div className="instrument-row__name">
              <strong>{summary.instrument}</strong>
              <span>{summary.currency}</span>
            </div>
            <div>
              <span>Trades</span>
              <strong>{summary.totalTrades}</strong>
            </div>
            <div>
              <span>Win rate</span>
              <strong>{summary.winRatePercentage}%</strong>
            </div>
            <div>
              <span>Avg. time</span>
              <strong>{formatMinutes(summary.averageTradeDurationMinutes)}</strong>
            </div>
            <div className="align-right">
              <span>Realized P/L</span>
              <strong
                className={`money money--${profitLossTone(
                  summary.totalRealizedProfitLoss,
                )}`}
              >
                {formatMoney(summary.totalRealizedProfitLoss, summary.currency)}
              </strong>
            </div>
          </article>
        ))}
      </div>
    </section>
  )
}
