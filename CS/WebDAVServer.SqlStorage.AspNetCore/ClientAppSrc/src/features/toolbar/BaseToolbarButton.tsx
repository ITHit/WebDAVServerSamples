import React from "react";
import { toolbarConfig, ButtonConfig } from "./settings";

type Props = {
  config: ButtonConfig;
  isDisabled: boolean;
  handleClick: () => void;
};
const BaseToolbarButton: React.FC<Props> = ({
  config,
  isDisabled,
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
      }`}
      title={config.title}
    >
      <i className={`icon ${config.iconClassName}`}></i>
      <span dangerouslySetInnerHTML={getInnerHtml()}></span>
    </button>
  );
};

export default BaseToolbarButton;
