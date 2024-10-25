import React from "react";
import { toolbarConfig, ButtonConfig } from "./settings";

type Props = {
  config: ButtonConfig;
  isDisabled: boolean;
  showing?: boolean;
  handleClick: () => void;
};
const BaseToolbarButton: React.FC<Props> = ({
  config,
  isDisabled,
  showing = true,
  handleClick,
}) => {
  const getInnerHtml = () => {
    return {
      __html: config.innerHtml,
    };
  };

  return (
    <button
      onClick={handleClick}
      disabled={isDisabled}
      id={config.name}
      className={`btn-tool ${
        toolbarConfig.hideDisabledOnMobile ? "hide-disabled-md" : ""
      } ${showing ? "" : "d-none"}`}
      title={config.title}
    >
      <i className={`icon ${config.iconClassName}`}></i>
      <span dangerouslySetInnerHTML={getInnerHtml()}></span>
    </button>
  );
};

export default BaseToolbarButton;
