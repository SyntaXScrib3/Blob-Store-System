import {useState, useCallback, useEffect} from "react";
import { Modal, Button, Form } from "react-bootstrap";

// eslint-disable-next-line react/prop-types
function CreateFolderModal({ show, onHide, onCreate }) {
   const [folderName, setFolderName] = useState("");

   // Reset folder name every time modal opens
   useEffect(() => {
      if (show) setFolderName("");
   }, [show]);

   const handleCreate = useCallback(() => {
      onCreate(folderName);
   }, [folderName, onCreate]);

   return (
      <Modal show={show} onHide={onHide} centered>
         <Modal.Header closeButton>
            <Modal.Title>Create Folder</Modal.Title>
         </Modal.Header>
         <Modal.Body>
            <Form.Group>
               <Form.Label>Folder Name</Form.Label>
               <Form.Control
                  type="text"
                  placeholder="Enter folder name"
                  value={folderName}
                  onChange={(e) => setFolderName(e.target.value)}
               />
            </Form.Group>
         </Modal.Body>
         <Modal.Footer>
            <Button variant="secondary" onClick={onHide}>
               Cancel
            </Button>
            <Button variant="primary" onClick={handleCreate}>
               Create
            </Button>
         </Modal.Footer>
      </Modal>
   );
}

export default CreateFolderModal;
