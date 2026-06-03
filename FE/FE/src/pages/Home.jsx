import React from 'react';
import Container from 'react-bootstrap/Container';
import Row from 'react-bootstrap/Row';
import Col from 'react-bootstrap/Col';
import Button from 'react-bootstrap/Button';
import MyNavbar from '../components/MyNavbar';
import MyFooter from '../components/MyFooter';

function Home() {
  // Thay link ảnh nền của bạn vào đây hoặc import từ thư mục assets
  const backgroundImage = 'https://i.pinimg.com/736x/2a/2a/33/2a2a337f02b6c63548fb8e03b24a796a.jpg';

  return (
    <>
    <div
      className="hero-section text-white d-flex align-items-center"
      style={{
        backgroundImage: `linear-gradient(rgba(10, 10, 20, 0.85), rgba(10, 10, 20, 0.85)), url(${backgroundImage})`,
        backgroundSize: 'cover',
        backgroundPosition: 'center',
        minHeight: '85vh',
        padding: '80px 0'
      }}
    >
      <Container>
        <Row>
          <Col lg={8} md={10} className="text-start">
            {/* Tag Badge nhỏ ở trên cùng */}
            <div className="mb-4">
              <span
                className="px-3 py-2 rounded-pill d-inline-flex align-items-center"
                style={{
                  backgroundColor: 'rgba(16, 185, 129, 0.15)',
                  color: '#10b981',
                  fontSize: '12px',
                  fontWeight: '600',
                  letterSpacing: '1px'
                }}
              >
                <span className="me-2" style={{ width: '6px', height: '6px', backgroundColor: '#10b981', borderRadius: '50%' }}></span>
                AI-POWERED CAREER PLATFORM
              </span>
            </div>

            {/* Tiêu đề chính */}
            <h1 className="fw-bold mb-3 display-4" style={{ lineHeight: '1.2' }}>
              Lộ trình nghề nghiệp <br />
              <span style={{ color: '#10b981' }}>được AI cá nhân hóa</span> <br />
              <span className="fw-light" style={{ fontStyle: 'italic' }}>chỉ dành cho bạn.</span>
            </h1>

            {/* Đoạn văn tả chi tiết */}
            <p className="text-secondary mb-5 fs-5 style-description" style={{ color: '#9ca3af', maxWidth: '650px', fontWeight: '300' }}>
              Hệ thống đánh giá năng lực ẩn, tạo lộ trình học tập động, kết nối mentor thực chiến —
              giúp sinh viên CNTT rút ngắn khoảng cách 2-3 năm giữa trường học và thị trường.
            </p>

            {/* Khu vực nút bấm */}
            <div className="d-flex flex-wrap gap-3 mb-5">
              <Button
                variant="success"
                className="px-4 py-2.5 fw-semibold d-flex align-items-center rounded-pill"
                style={{ backgroundColor: '#10b981', border: 'none' }}
              >
                <i className="bi bi-cursor-fill me-2"></i> Bắt đầu miễn phí
              </Button>

              <Button
                variant="outline-light"
                className="px-4 py-2.5 fw-semibold d-flex align-items-center rounded-pill"
                style={{ borderColor: 'rgba(255,255,255,0.3)' }}
              >
                <i className="bi bi-play-circle me-2"></i> Xem cách hoạt động
              </Button>
            </div>

            {/* Khu vực thông số thống kê ở dưới cùng */}
            <Row className="mt-5 pt-4 border-top" style={{ borderColor: 'rgba(255,255,255,0.1) !important' }}>
              <Col xs={6} sm={3} className="mb-3">
                <h3 className="fw-bold m-0" style={{ color: '#10b981' }}>12,000+</h3>
                <small className="text-nowrap" >Sinh viên đang học</small>
              </Col>
              <Col xs={6} sm={3} className="mb-3">
                <h3 className="fw-bold m-0" style={{ color: '#10b981' }}>94%</h3>
                <small className="text-nowrap">Có việc sau 6 tháng</small>
              </Col>
              <Col xs={6} sm={3} className="mb-3">
                <h3 className="fw-bold m-0" style={{ color: '#10b981' }}>320+</h3>
                <small className="text-nowrap">Mentor ngành</small>
              </Col>
              <Col xs={6} sm={3} className="mb-3">
                <h3 className="fw-bold m-0" style={{ color: '#10b981' }}>50+</h3>
                <small className="text-nowrap">Khám phá lộ trình AI</small>
              </Col>
            </Row>

          </Col>
        </Row>
      </Container>
    </div>
    <MyFooter />
    </>
  );
}

export default Home;
