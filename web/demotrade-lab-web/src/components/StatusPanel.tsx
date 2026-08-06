interface StatusPanelProps {
  eyebrow: string
  title: string
  message: string
  tone?: 'default' | 'error'
  actionLabel?: string
  onAction?: () => void
}

export function StatusPanel({
  eyebrow,
  title,
  message,
  tone = 'default',
  actionLabel,
  onAction,
}: StatusPanelProps) {
  return (
    <section className={`status-panel status-panel--${tone}`} role="status">
      <span className="status-panel__eyebrow">{eyebrow}</span>
      <h2>{title}</h2>
      <p>{message}</p>
      {actionLabel && onAction ? (
        <button className="button button--secondary" type="button" onClick={onAction}>
          {actionLabel}
        </button>
      ) : null}
    </section>
  )
}
