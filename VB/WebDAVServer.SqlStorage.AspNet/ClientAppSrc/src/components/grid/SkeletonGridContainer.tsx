import { SkeletonRow } from './SkeletonRow';

const ROW_COUNT = 5;
const rows = Array.from({ length: ROW_COUNT }, (_, i) => i);

export function SkeletonGridContainer() {
  return (
    <tbody className="bg-surface divide-y divide-border">
      {rows.map(index => (
        <SkeletonRow key={index} index={index} />
      ))}
    </tbody>
  );
}
