import type { InstrumentSummary, TradeFilters as Filters } from '../api/types'

interface TradeFiltersProps {
  filters: Filters
  instruments: InstrumentSummary[]
  onChange: (filters: Filters) => void
  onReset: () => void
}

export function TradeFilters({
  filters,
  instruments,
  onChange,
  onReset,
}: TradeFiltersProps) {
  const instrumentOptions = uniqueSorted(
    instruments.map((summary) => summary.instrument),
  )
  const currencyOptions = uniqueSorted(
    instruments.map((summary) => summary.currency),
  )

  return (
    <div className="filters" aria-label="Trade filters">
      <label>
        Instrument
        <select
          value={filters.instrument}
          onChange={(event) =>
            onChange({ ...filters, instrument: event.currentTarget.value })
          }
        >
          <option value="">All instruments</option>
          {instrumentOptions.map((instrument) => (
            <option key={instrument} value={instrument}>
              {instrument}
            </option>
          ))}
        </select>
      </label>

      <label>
        Currency
        <select
          value={filters.currency}
          onChange={(event) =>
            onChange({ ...filters, currency: event.currentTarget.value })
          }
        >
          <option value="">All currencies</option>
          {currencyOptions.map((currency) => (
            <option key={currency} value={currency}>
              {currency}
            </option>
          ))}
        </select>
      </label>

      <label>
        Direction
        <select
          value={filters.direction}
          onChange={(event) =>
            onChange({
              ...filters,
              direction: event.currentTarget.value as Filters['direction'],
            })
          }
        >
          <option value="">Buy & sell</option>
          <option value="buy">Buy</option>
          <option value="sell">Sell</option>
        </select>
      </label>

      <label>
        Outcome
        <select
          value={filters.outcome}
          onChange={(event) =>
            onChange({
              ...filters,
              outcome: event.currentTarget.value as Filters['outcome'],
            })
          }
        >
          <option value="">All outcomes</option>
          <option value="profitable">Profitable</option>
          <option value="losing">Losing</option>
          <option value="breakEven">Break-even</option>
        </select>
      </label>

      <label>
        Sort by
        <select
          value={filters.sortBy}
          onChange={(event) =>
            onChange({
              ...filters,
              sortBy: event.currentTarget.value as Filters['sortBy'],
            })
          }
        >
          <option value="closedAtUtc">Closing time</option>
          <option value="openedAtUtc">Opening time</option>
          <option value="instrument">Instrument</option>
          <option value="realizedProfitLoss">Profit / loss</option>
          <option value="duration">Duration</option>
        </select>
      </label>

      <label>
        Order
        <select
          value={filters.sortDirection}
          onChange={(event) =>
            onChange({
              ...filters,
              sortDirection: event.currentTarget.value as Filters['sortDirection'],
            })
          }
        >
          <option value="descending">Descending</option>
          <option value="ascending">Ascending</option>
        </select>
      </label>

      <button className="button button--ghost filters__reset" type="button" onClick={onReset}>
        Reset filters
      </button>
    </div>
  )
}

function uniqueSorted(values: string[]): string[] {
  return [...new Set(values)].sort((left, right) => left.localeCompare(right))
}
