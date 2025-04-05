import { useEffect, useState } from "react";
import { Modal, Button, Card } from "react-bootstrap";
import PathNavigator from "../PathNavigator.jsx";
import api from "../../../../services/api.js";

/**
 * A modal allowing the user to move or copy selected items.
 */
// eslint-disable-next-line react/prop-types
function MoveCopyModal({ show, mode, onClose, onComplete, authAxios, selectedNodes }) {
   const [rootPath, setRootPath] = useState("/");
   const [listContent, setListContent] = useState([]);
   const [loading, setLoading] = useState(false);

   useEffect(() => {
      if (show) {
         listDirectoryItems(rootPath);
      }
   }, [show, rootPath]);


   // Lists only directories at the given path.
   const listDirectoryItems = async (dirPath) => {
      setLoading(true);
      try {
         const resp = await api.List(dirPath, authAxios)
         const dirsOnly = resp.data.filter((node) => node.isDirectory);
         setListContent(dirsOnly);
      } catch (err) {
         console.error("Error listing directories:", err);
      } finally {
         setLoading(false);
      }
   };

   // If a user clicks on a directory in the modal, we descend into that folder.
   const handleDirClick = (node) => {
      if (node.isDirectory) {
         setRootPath(node.path);
      }
   };

   // Check if the user is trying to move/copy a folder into itself or its subfolder
   const isInvalidDestination = checkInvalidDestination(rootPath, selectedNodes);

   const handleConfirm = () => {
      if (!isInvalidDestination) {
         onComplete(rootPath);
      }
   };

   return (
      <Modal show={show} onHide={onClose} size="lg" centered>
         <Modal.Header closeButton>
            <Modal.Title>{mode === "move" ? "Move To" : "Copy To"}</Modal.Title>
         </Modal.Header>

         <Modal.Body>
            <PathNavigator path={rootPath} onNavigate={setRootPath} />

            {loading ? (
               <p>Loading...</p>
            ) : (
               <Card style={{ backgroundColor: "#f8f9fa" }}>
                  {listContent.length === 0 ? (
                     <p
                        style={{
                           margin: "50px 0",
                           display: "flex",
                           justifyContent: "center",
                           alignItems: "center",
                        }}
                     >
                        This folder is empty
                     </p>
                  ) : (
                     <Card.Body>
                        {listContent.map((node) => (
                           <div
                              key={node.id}
                              className="border-bottom py-2"
                              style={{ cursor: "pointer" }}
                              onClick={() => handleDirClick(node)}
                           >
                              <strong>{node.name}</strong>{" "}
                              <span className="text-muted">(dir)</span>
                           </div>
                        ))}
                     </Card.Body>
                  )}
               </Card>
            )}

            {isInvalidDestination && (
               <div className="alert alert-warning mt-3" role="alert">
                  You cannot {mode} a folder into itself or one of its subfolders.
               </div>
            )}
         </Modal.Body>

         <Modal.Footer>
            <Button variant="secondary" onClick={onClose}>
               Cancel
            </Button>
            <Button
               variant="primary"
               onClick={handleConfirm}
               disabled={isInvalidDestination}
            >
               {mode === "move" ? "Move Here" : "Copy Here"}
            </Button>
         </Modal.Footer>
      </Modal>
   );
}

/**
 * Returns true if user is trying to move/copy a directory into itself or subfolder
 */
function checkInvalidDestination(modalPath, selectedNodes) {
   const selectedDirs = selectedNodes.filter((node) => node.isDirectory);

   for (const dirNode of selectedDirs) {
      const dirPath = dirNode.path;
      if (modalPath === dirPath || modalPath.startsWith(dirPath + "/")) {
         return true;
      }
   }
   return false;
}

export default MoveCopyModal;