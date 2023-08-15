import React from "react";
import { ITHit } from "webdav.client";
import Breadcrumb from "../Breadcrumb";
import { snippetPropertyName } from "../../services/WebDavService";
import { CommonService } from "../../services/CommonService";
type Props = {
  item: ITHit.WebDAV.Client.HierarchyItem;
};
const Snippet: React.FC<Props> = ({ item }) => {
  const getSnippet = () => {
    let snippet = CommonService.formatSnippet(
      item.Properties.Find(snippetPropertyName)
    );
    return snippet ? snippet : "";
  };

  const getHtml = (html: string) => {
    return {
      __html: html,
    };
  };
  return (
    <div>
      <div>
        <Breadcrumb isSearchMode={true} itemUrl={item.Href} />
      </div>
      <div
        className="snippet"
        dangerouslySetInnerHTML={getHtml(getSnippet())}
      />
    </div>
  );
};

export default Snippet;
