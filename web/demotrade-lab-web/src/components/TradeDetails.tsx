import type { Trade } from '../api/types'
import type { Loadable } from '../hooks/useApiData'
import {
  formatDateTime,
  formatDirection,
  formatDuration,
  formatMoney,
  formatNumber,
  profitLossTone,
} from '../utils/format'

interface TradeDetailsProps {
  trade: Loadable<Trade>
  selectedTradeId: string | null
  onClose: () => void
}

export function TradeDetails({
  trade,
  selectedTradeId,
  onClose,
}: TradeDetailsProps) {
  if (!selectedTradeId) {
    return (
      <aside className="details-panel details-panel--empty">
        <span className="details-panel__mark">↗</span>
        <h3>Select a trade</h3>
        <p>Open a row to inspect prices, timing, costs, source, and identity.</p>
      </aside>
    )
  }

  if (trade.isLoading) {
    return (
      <aside className="details-panel" aria-busy="true">
        <span className="eyebrow">Trade details</span>
        <h3>Loading the selected trade…</h3>
        <div className="skeleton skeleton--tall" />
      </aside>
    )
  }

  if (trade.error || !trade.data) {
    return (
      <aside className="details-panel">
        <button className="details-panel__close" type="button" onClick={onClose}>
          Close
        </button>
        <span className="eyebrow">Unable to load</span>
        <h3>Trade details unavailable</h3>
        <p>{trade.error ?? 'The selected trade no longer exists.'}</p>
      </aside>
    )
  }

  const data = trade.data

  return (
    <aside className="details-panel">
      <div className="details-panel__header">
        <div>
          <span className="eyebrow">Trade details</span>
          <h3>{data.instrument}</h3>
        </div>
        <button className="details-panel__close" type="button" onClick={onClose}>
          Close
        </button>
      </div>

      <div className="details-panel__result">
        <span className={`direction direction--${data.direction}`}>
          {formatDirection(data.direction)}
        </span>
        <strong className={`money money--${profitLossTone(data.realizedProfitLoss)}`}>
          {formatMoney(data.realizedProfitLoss, data.currency)}
        </strong>
      </div>

      <dl className="details-list">
        <Detail label="Opened" value={formatDateTime(data.openedAtUtc)} />
        <Detail label="Closed" value={formatDateTime(data.closedAtUtc)} />
        <Detail
          label="Duration"
          value={formatDuration(data.openedAtUtc, data.closedAtUtc)}
        />
        <Detail label="Opening price" value={formatNumber(data.openingPrice, 8)} />
        <Detail label="Closing price" value={formatNumber(data.closingPrice, 8)} />
        <Detail label="Quantity" value={formatNumber(data.quantity, 8)} />
        <Detail
          label="Fees"
          value={data.fees === null ? 'Not recorded' : formatMoney(-data.fees, data.currency)}
        />
        <Detail
          label="Financing"
          value={
            data.financingCosts === null
              ? 'Not recorded'
              : formatMoney(-data.financingCosts, data.currency)
          }
        />
        <Detail label="Source" value={data.source} />
      </dl>

      <div className="details-panel__identity">
        <span>Trade ID</span>
        <code>{data.id}</code>
      </div>
    </aside>
  )
}

function Detail({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  )
}
