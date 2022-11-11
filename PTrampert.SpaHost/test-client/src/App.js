import 'bootstrap/dist/css/bootstrap.min.css';
import './App.css';
import AppNav from "./AppNav";
import Container from 'react-bootstrap/Container';
import Row from 'react-bootstrap/Row';
import UserInfo from "./UserInfo";
import {useEffect, useState} from "react";
import {Button} from "react-bootstrap";

async function loadUserInfo(setUserInfo) {
  let response = await fetch('/UserInfo', {credentials: "include"});
  if (response.ok) {
    let userInfo = await response.json();
    setUserInfo(userInfo);
  }
}

function App() {
  let [userInfo, setUserInfo] = useState([]);

  useEffect(() => {
    loadUserInfo(setUserInfo);
  }, []);

  return (
    <div className="App">
      <Container>
        <Row>
          <AppNav/>
          {userInfo.length ? <UserInfo userInfo={userInfo}/> : (
            <form action="/login" method="POST">
              <Button type="submit">Login</Button>
            </form>
          )}
        </Row>
      </Container>
    </div>
  );
}

export default App;
