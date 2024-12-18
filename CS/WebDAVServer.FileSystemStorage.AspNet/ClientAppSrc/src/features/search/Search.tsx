import React, { useState, useEffect } from "react";
import Snippet from "./Snippet";
import { ITHit } from "webdav.client";
import { useAppSelector, useAppDispatch } from "../../app/hooks/common";
import {
  clearSearchedItems,
  getOptionsInfo,
  getSearchedItems,
  setSearchedItems,
  setSearchedItem,
  setSearchQuery,
  getSearchQuery,
  getSearchMode,
} from "../grid/gridSlice";
import { useTranslation } from "react-i18next";
import { SearchParams } from "../../models/SearchParams";
import useDebounce from "../../app/hooks/useDebounce";
import { StoreWorker } from "../../app/storeWorker";

const Search: React.FC = () => {
  const { t } = useTranslation();
  const dispatch = useAppDispatch();

  const maxShowItems = 6;
  const searchedItems = useAppSelector(getSearchedItems);
  const optionsInfo = useAppSelector(getOptionsInfo);
  const storeSearchQuery = useAppSelector(getSearchQuery);
  const searchMode = useAppSelector(getSearchMode);
  const [query, setQuery] = useState("");
  const [mouseOverMenuDisplayed, setMouseOverMenuDisplayed] = useState(false);
  const [isFocusInput, setIsFocusInput] = useState(false);
  const [showMenu, setShowMenu] = useState(false);
  const [isFirstRender, setIsFirstRender] = useState(true);

  const debouncedQuery = useDebounce<string>(query, 400);

  const isDisabled = () => {
    let res = true;
    if (optionsInfo) {
      res = !(optionsInfo.Features & ITHit.WebDAV.Client.Features.Dasl);
    }

    return res;
  };

  const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const enteredQuery = event.target.value;
    setQuery(enteredQuery);
  };

  const handleKeyPress = (event: React.KeyboardEvent) => {
    if (event.key === "Enter") {
      dispatch(setSearchQuery(query));
      StoreWorker.refresh();
    }
  };

  const clearSearch = () => {
    setQuery("");
    if (searchMode) {
      dispatch(setSearchQuery(""));
      StoreWorker.refresh();
    }
  };

  const selectSearchedItem = (item: ITHit.WebDAV.Client.HierarchyItem) => {
    dispatch(setSearchedItem(item));

    setQuery(item.DisplayName);
    setShowMenu(false);
    setMouseOverMenuDisplayed(false);
  };

  useEffect(() => {
    if (searchedItems.length && (mouseOverMenuDisplayed || isFocusInput)) {
      setShowMenu(true);
    } else {
      setShowMenu(false);
    }
  }, [mouseOverMenuDisplayed, isFocusInput, searchedItems]);

  useEffect(() => {
    if (debouncedQuery) {
      dispatch(setSearchedItems(new SearchParams(debouncedQuery, maxShowItems)));
    } else {
      dispatch(clearSearchedItems());
    }
  }, [debouncedQuery, dispatch]);

  useEffect(() => {
    if (isFirstRender && storeSearchQuery) {
      setQuery(storeSearchQuery);
      setIsFirstRender(false);
    }
  }, [isFirstRender, storeSearchQuery]);

  useEffect(() => {});

  return (
    <div className="ithit-search-container">
      <div className="twitter-typeahead">
        <input
          value={query}
          className="form-control"
          disabled={isDisabled()}
          placeholder={isDisabled() ? t("phrases.validations.notSupportSearch") : ""}
          onChange={handleInputChange}
          onKeyPress={handleKeyPress}
          onFocus={() => setIsFocusInput(true)}
          onBlur={() => setIsFocusInput(false)}
        />
        {query.length > 0 && (
          <button className="btn-transparent btn-clear-search" onClick={() => clearSearch()}>
            <i className="icon icon-close"></i>
          </button>
        )}
      </div>
      {showMenu && (
        <div
          className="tt-menu"
          style={{
            position: "absolute",
            top: "100%",
            left: "0px",
            zIndex: 999,
          }}
          onMouseEnter={() => setMouseOverMenuDisplayed(true)}
          onMouseLeave={() => setMouseOverMenuDisplayed(false)}
        >
          <div className="tt-dataset tt-dataset-states">
            {searchedItems.map((item, i) => {
              return (
                <div
                  key={i}
                  className="tt-suggestion tt-selectable"
                  onClick={() => {
                    selectSearchedItem(item);
                  }}
                >
                  {item.DisplayName}
                  <Snippet item={item} />
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
};

export default Search;
