interface Props {
  fileExtension: string;
}

export function UploadingFileIcon({ fileExtension }: Props) {
  const iconClass = `icon icon-file icon-file-${fileExtension.toLowerCase()} relative`;
  const shouldShowExtension = fileExtension.length < 5;

  return (
    <div className={iconClass} style={{ width: 38, height: 49 }}>
      {shouldShowExtension ? (
        <span className="absolute bottom-0 left-1/2 -translate-x-1/2 text-xs leading-[1.3] text-gray-200 dark:text-gray-700">
          {fileExtension.toUpperCase()}
        </span>
      ) : null}
    </div>
  );
}
