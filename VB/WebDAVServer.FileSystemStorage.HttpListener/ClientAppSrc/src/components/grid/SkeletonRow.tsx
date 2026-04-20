const WIDTHS = ['75%', '60%', '85%', '65%', '70%'];

interface Props {
  index?: number;
}

export function SkeletonRow({ index = 0 }: Props) {
  const nameWidth = WIDTHS[index % WIDTHS.length];

  return (
    <tr className="border-b border-border">
      <td className="w-10 px-4 py-2 text-right">
        <span className="skeleton-loader h-4 w-4 block" />
      </td>
      <td className="w-10 px-2 py-2">
        <span className="skeleton-loader h-4 w-8 block" />
      </td>
      <td className="px-4 py-2 max-w-0">
        <span className="skeleton-loader h-4 block" style={{ width: nameWidth }} />
      </td>
      <td className="hidden xl:table-cell w-32 px-4 py-2">
        <span className="skeleton-loader h-4 w-20 block" />
      </td>
      <td className="hidden xl:table-cell w-40 px-4 py-2">
        <span className="skeleton-loader h-4 w-16 block" />
      </td>
      <td className="hidden lg:table-cell w-60 px-4 py-2">
        <span className="skeleton-loader h-4 w-28 block" />
      </td>
      <td className="w-16 px-4 py-2 text-right">
        <span className="skeleton-loader h-4 w-6 block" />
      </td>
    </tr>
  );
}
