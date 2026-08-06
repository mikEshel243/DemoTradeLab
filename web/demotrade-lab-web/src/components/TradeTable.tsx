import type { Trade } from '../api/types'
import {
  formatDateTime,
  formatDirection,
  formatDuration,
  formatMoney,
  profitLossTone,
} from '../utils/format'

interface TradeTableProps {
  trades: Trade[]
  selectedTradeId: string | null
  onSelect: (id: string) => void
}

export function TradeTable({ trades, selectedTradeId, onSelect }: TradeTableProps) {
  return (
    <div className="table-scroll">
      <table className="trade-table">
        <thead>
          <tr>
            <th>Instrument</th>
            <th>Direction</th>
            <th>Closed</th>
            <th>Duration</th>
            <th>Source</th>
            <th className="align-right">Realized P/L</th>
          </tr>
        </thead>
        <tbody>
          {trades.map((trade) => (
            <tr
              className={selectedTradeId === trade.id ? 'is-selected' : undefined}
              key={trade.id}
            >
              <td>
                <button
                  className="instrument-button"
                  type="button"
                  onClick={() => onSelect(trade.id)}
                >
                  <strong>{trade.instrument}</strong>
                  <span>View details</span>
                </button>
              </td>
              <td>
                <span className={`direction direction--${trade.direction}`}>
                  {formatDirection(trade.direction)}
                </span>
              </td>
              <td>{formatDateTime(trade.closedAtUtc)}</td>
              <td>{formatDuration(trade.openedAtUtc, trade.closedAtUtc)}</td>
              <td>
                <span className="source-chip">{trade.source}</span>
              </td>
              <td
                className={`align-right money money--${profitLossTone(
                  trade.realizedProfitLoss,
                )}`}
              >
                {formatMoney(trade.realizedProfitLoss, trade.currency)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
