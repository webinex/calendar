import React from "react";
import ReactDOM from "react-dom";
import "./index.css";
import App from "./App";
import moment from 'moment';

ReactDOM.render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,

  document.getElementById("root") as HTMLElement
);

moment.updateLocale('en-US', {
  week: {
    dow: 1
  }
})
