import React from "react";

type Props = {
  title: string;
  dialogClassName?: string;
  closeModal: () => void;
};
const DefaultModal: React.FC<Props> = ({
  children,
  title,
  closeModal,
  dialogClassName = "modal-md",
}) => {
  return (
    <div>
      <div className="modal show" role="dialog">
        <div className={`modal-dialog ${dialogClassName}`} role="document">
          <div className="modal-content">
            <div className="modal-header">
              <h5 className="modal-title">{title}</h5>
              <button
                type="button"
                className="btn-close"
                aria-label="Close"
                onClick={closeModal}
              ></button>
            </div>
            {children}
          </div>
        </div>
      </div>
      <div className="modal-backdrop show" />
    </div>
  );
};

export default DefaultModal;
