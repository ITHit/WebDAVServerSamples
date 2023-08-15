import React, { useEffect } from "react";
import { ButtonConfig } from "./settings";
import { UploadService } from "../../services/UploadService";
type Props = {
  config: ButtonConfig;
  inputId: string;
};
const UploadInput: React.FC<Props> = ({ config, inputId }) => {
  const getInnerHtml = () => {
    return {
      __html: config.innerHtml,
    };
  };

  useEffect(() => {
    UploadService.addInput(inputId);
  });

  return (
    <>
      <label className="btn-tool" title={config.title} htmlFor={inputId}>
        <i className={`icon ${config.iconClassName}`}></i>
        <span dangerouslySetInnerHTML={getInnerHtml()}></span>
      </label>
      <input
        id="ithit-button-input"
        className="d-none"
        type="file"
        multiple
        hidden
      />
    </>
  );
};
export default UploadInput;
