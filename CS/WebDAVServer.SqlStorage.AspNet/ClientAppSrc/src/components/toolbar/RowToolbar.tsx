import type { ResolvedRowToolbarItem } from '@/shared/config/config-types';

interface Props {
  items: ResolvedRowToolbarItem[];
}

export function RowToolbar({ items }: Props) {
  return (
    <div className="opacity-0 group-hover:opacity-100 transition-opacity duration-150 items-center gap-0.5 hidden md:flex ml-auto">
      {items.map(button => (
        <button
          key={button.id}
          type="button"
          className="p-1.5 rounded hover:bg-surface-active text-muted cursor-pointer disabled:opacity-40 disabled:cursor-not-allowed"
          title={button.title}
          disabled={button.disabled}
          onClick={event => {
            event.stopPropagation();
            void button.action();
          }}
        >
          <i className={`icon w-4 h-4 align-middle ${button.icon}`} />
        </button>
      ))}
    </div>
  );
}
