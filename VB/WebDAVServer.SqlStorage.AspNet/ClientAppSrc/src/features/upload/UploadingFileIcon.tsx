import React from "react";

type Props = { fileExtension: string };
const UploadingFileIcon: React.FC<Props> = ({ fileExtension }) => {
  return (
    <div className={`icon icon-file icon-file-${fileExtension.toLowerCase()}`}>
      {fileExtension.length < 5 && (
        <span className="file-extension">{fileExtension.toUpperCase()}</span>
      )}
    </div>
  );
};

export default UploadingFileIcon;
