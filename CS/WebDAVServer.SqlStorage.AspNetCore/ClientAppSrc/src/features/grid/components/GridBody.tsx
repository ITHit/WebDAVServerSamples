import React from "react";
import { useAppSelector } from "../../../app/hooks/common";
import { getItems } from "../gridSlice";
import GridRow from "./GridRow";

type Props = {};
const GridBody: React.FC<Props> = () => {
  const items = useAppSelector(getItems);

  return (
    <tbody>
      {items.map((item, i) => {
        return <GridRow item={item} index={i} key={"item-" + i} />;
      })}
    </tbody>
  );
};

export default GridBody;
