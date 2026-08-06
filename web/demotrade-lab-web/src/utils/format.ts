import type { TradeDirection } from '../api/types'

export function formatMoney(value: number, currency: string): string {
  const absoluteValue = new Intl.NumberFormat(undefined, {
    style: 'currency',
    currency,
    maximumFractionDigits: 2,
  }).format(Math.abs(value))

  if (value > 0) return `+${absoluteValue}`
  if (value < 0) return `-${absoluteValue}`
  return absoluteValue
}

export function formatNumber(value: number, maximumFractionDigits = 4): string {
  return new Intl.NumberFormat(undefined, {
    maximumFractionDigits,
  }).format(value)
}

export function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

export function formatDuration(openedAtUtc: string, closedAtUtc: string): string {
  const durationMinutes =
    (new Date(closedAtUtc).getTime() - new Date(openedAtUtc).getTime()) / 60_000

  return formatMinutes(durationMinutes)
}

export function formatMinutes(value: number | null): string {
  if (value === null) return '—'
  if (value < 60) return `${formatNumber(value, 1)} min`

  const hours = Math.floor(value / 60)
  const minutes = Math.round(value % 60)
  return minutes === 0 ? `${hours} hr` : `${hours} hr ${minutes} min`
}

export function formatDirection(direction: TradeDirection): string {
  return direction === 'buy' ? 'Buy' : 'Sell'
}

export function profitLossTone(value: number): 'positive' | 'negative' | 'neutral' {
  if (value > 0) return 'positive'
  if (value < 0) return 'negative'
  return 'neutral'
}
