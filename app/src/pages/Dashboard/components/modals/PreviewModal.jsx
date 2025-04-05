import { Modal, Button } from "react-bootstrap";

// eslint-disable-next-line react/prop-types
function PreviewModal({ show, previewData, onClose }) {
   // eslint-disable-next-line react/prop-types
   const { content, fileType, fileName } = previewData;

   return (
      <Modal show={show} onHide={onClose} size="lg" centered>
         <Modal.Header closeButton>
            <Modal.Title>{fileName}</Modal.Title>
         </Modal.Header>
         <Modal.Body>
            {/* eslint-disable-next-line react/prop-types */}
            {fileType.startsWith("image/") && content ? (
               <img
                  src={content}
                  alt={fileName}
                  style={{ maxWidth: "100%" }}
               />
               // eslint-disable-next-line react/prop-types
            ) : fileType.startsWith("text/") ? (
               <pre style={{ whiteSpace: "pre-wrap" }}>{content}</pre>
            ) : (
               <div>
                  <p>Preview not supported for this file type.</p>
                  {content && (
                     <a href={content} download={fileName} className="btn btn-primary">
                        Download File
                     </a>
                  )}
               </div>
            )}
         </Modal.Body>
         <Modal.Footer>
            <Button variant="secondary" onClick={onClose}>
               Close
            </Button>
         </Modal.Footer>
      </Modal>
   );
}

export default PreviewModal;
