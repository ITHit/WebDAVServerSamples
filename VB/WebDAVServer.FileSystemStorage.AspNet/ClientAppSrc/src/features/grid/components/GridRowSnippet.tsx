import React from "react";
import Snippet from "../../search/Snippet";
import { ITHit } from "webdav.client";

type Props = { item: ITHit.WebDAV.Client.HierarchyItem };
const GridRowSnippet: React.FC<Props> = ({ item }) => {
  return (
    <tr className="tr-snippet-url">
      <td className="d-none d-xl-table-cell" />
      <td className="d-none d-lg-table-cell" />
      <td className="10">
        <Snippet item={item} />
      </td>
    </tr>
  );
};

export default GridRowSnippet;
