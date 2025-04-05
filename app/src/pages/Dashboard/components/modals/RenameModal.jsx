import { useState, useEffect } from "react";
import { Modal, Button, Form } from "react-bootstrap";

// eslint-disable-next-line react/prop-types
function RenameModal({ show, isDirectory, originalName, onHide, onConfirm, }) {
   const [newName, setNewName] = useState(originalName);

   useEffect(() => {
      setNewName(originalName);
   }, [originalName]);

   const handleConfirm = () => {
      onConfirm(newName);
   };

   return (
      <Modal show={show} onHide={onHide} centered>
         <Modal.Header closeButton>
            <Modal.Title>Rename {isDirectory ? "Folder" : "File"}</Modal.Title>
         </Modal.Header>
         <Modal.Body>
            <Form.Group>
               <Form.Label>New Name</Form.Label>
               <Form.Control
                  type="text"
                  value={newName}
                  onChange={(e) => setNewName(e.target.value)}
               />
            </Form.Group>
         </Modal.Body>
         <Modal.Footer>
            <Button variant="secondary" onClick={onHide}>
               Cancel
            </Button>
            <Button variant="primary" onClick={handleConfirm}>
               Rename
            </Button>
         </Modal.Footer>
      </Modal>
   );
}

export default RenameModal;
