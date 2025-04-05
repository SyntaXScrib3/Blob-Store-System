import React from "react";
import getPathItems from "../../../utils/pathUtils.js";

// eslint-disable-next-line react/prop-types
function PathNavigator({ path, onNavigate }) {
    const pathItems = getPathItems(path);

    return (
       <div style={{ padding: "8px 16px" }}>
           {pathItems.map((item, index) => (
              <React.Fragment key={item.path}>
          <span
             onClick={() => onNavigate(item.path)}
             style={{
                 cursor: "pointer",
                 fontWeight: "bold",
                 marginRight: "5px",
             }}
          >
            {item.label}
          </span>
                  {index < pathItems.length - 1 && <span> &gt; </span>}
              </React.Fragment>
           ))}
       </div>
    );
}

export default PathNavigator;
