import React from "react";
import InnerContainer from "./InnerContainer";
import Header from "./Header";

const MainContainer: React.FC = () => {
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
