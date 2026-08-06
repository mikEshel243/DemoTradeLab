import { useState } from 'react'
import type { CurrencyProfitLossTimeline } from '../api/types'
import { formatDateTime, formatMoney, profitLossTone } from '../utils/format'

interface ProfitLossTimelineProps {
  timelines: CurrencyProfitLossTimeline[]
}

interface ChartPoint {
  x: number
  y: number
  value: number
}

const chartWidth = 720
const chartHeight = 240
const chartPadding = 24

export function ProfitLossTimeline({ timelines }: ProfitLossTimelineProps) {
  const [requestedCurrency, setRequestedCurrency] = useState('')
  const timeline =
    timelines.find((item) => item.currency === requestedCurrency) ?? timelines[0]

  if (!timeline || timeline.points.length === 0) {
    return (
      <section className="panel timeline-panel">
        <span className="eyebrow">Cumulative result</span>
        <h2>Profit / loss timeline</h2>
        <p className="muted-copy">Timeline data will appear after trades are added.</p>
      </section>
    )
  }

  const points = createChartPoints(timeline)
  const finalValue = timeline.points.at(-1)?.cumulativeRealizedProfitLoss ?? 0
  const values = timeline.points.map((point) => point.cumulativeRealizedProfitLoss)
  const maximum = Math.max(0, ...values)
  const minimum = Math.min(0, ...values)

  return (
    <section className="panel timeline-panel" aria-labelledby="timeline-title">
      <div className="panel__header">
        <div>
          <span className="eyebrow">Cumulative result</span>
          <h2 id="timeline-title">Profit / loss timeline</h2>
        </div>
        {timelines.length > 1 ? (
          <label className="compact-select">
            Currency
            <select
              value={timeline.currency}
              onChange={(event) => setRequestedCurrency(event.currentTarget.value)}
            >
              {timelines.map((item) => (
                <option key={item.currency} value={item.currency}>
                  {item.currency}
                </option>
              ))}
            </select>
          </label>
        ) : (
          <span className="currency-chip">{timeline.currency}</span>
        )}
      </div>

      <div className="timeline-summary">
        <strong className={`money money--${profitLossTone(finalValue)}`}>
          {formatMoney(finalValue, timeline.currency)}
        </strong>
        <span>{timeline.points.length} closing events</span>
      </div>

      <div className="chart-shell">
        <span className="chart-label chart-label--top">
          {formatMoney(maximum, timeline.currency)}
        </span>
        <svg
          className="timeline-chart"
          viewBox={`0 0 ${chartWidth} ${chartHeight}`}
          role="img"
          aria-label={`Cumulative ${timeline.currency} realized profit and loss`}
        >
          <defs>
            <linearGradient id="area-fill" x1="0" x2="0" y1="0" y2="1">
              <stop offset="0%" stopColor="#45b88a" stopOpacity="0.35" />
              <stop offset="100%" stopColor="#45b88a" stopOpacity="0" />
            </linearGradient>
          </defs>
          <line
            className="chart-zero-line"
            x1={chartPadding}
            x2={chartWidth - chartPadding}
            y1={valueToY(0, minimum, maximum)}
            y2={valueToY(0, minimum, maximum)}
          />
          <polygon
            className="chart-area"
            points={`${points.map((point) => `${point.x},${point.y}`).join(' ')} ${
              points.at(-1)?.x
            },${chartHeight - chartPadding} ${points[0]?.x},${
              chartHeight - chartPadding
            }`}
          />
          <polyline
            className="chart-line"
            points={points.map((point) => `${point.x},${point.y}`).join(' ')}
          />
          {points.map((point, index) => (
            <circle className="chart-point" cx={point.x} cy={point.y} key={index} r="5">
              <title>
                {formatDateTime(timeline.points[index].closedAtUtc)} —{' '}
                {formatMoney(point.value, timeline.currency)}
              </title>
            </circle>
          ))}
        </svg>
        <span className="chart-label chart-label--bottom">
          {formatMoney(minimum, timeline.currency)}
        </span>
      </div>

      <div className="timeline-dates">
        <span>{formatDateTime(timeline.points[0].closedAtUtc)}</span>
        <span>{formatDateTime(timeline.points.at(-1)?.closedAtUtc ?? '')}</span>
      </div>
    </section>
  )
}

function createChartPoints(timeline: CurrencyProfitLossTimeline): ChartPoint[] {
  const values = timeline.points.map((point) => point.cumulativeRealizedProfitLoss)
  const maximum = Math.max(0, ...values)
  const minimum = Math.min(0, ...values)
  const usableWidth = chartWidth - chartPadding * 2

  return timeline.points.map((point, index) => ({
    x:
      timeline.points.length === 1
        ? chartWidth / 2
        : chartPadding + (index / (timeline.points.length - 1)) * usableWidth,
    y: valueToY(point.cumulativeRealizedProfitLoss, minimum, maximum),
    value: point.cumulativeRealizedProfitLoss,
  }))
}

function valueToY(value: number, minimum: number, maximum: number): number {
  const range = maximum - minimum || 1
  const usableHeight = chartHeight - chartPadding * 2
  return chartPadding + ((maximum - value) / range) * usableHeight
}
