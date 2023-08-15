import React from "react";
import InnerContainer from "./InnerContainer";
import Header from "./Header";

type Props = {};
const MainContainer: React.FC<Props> = () => {
  return (
    <div>
      <Header />
      <div className="container-fluid">
        <InnerContainer />
      </div>
    </div>
  );
};

export default MainContainer;
