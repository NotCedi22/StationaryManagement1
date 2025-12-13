# HMT Technologies - Stationery Management System
## Project Documentation

### Overview
This web-based Stationery Management System is designed for HMT Technologies to efficiently track and manage stationery requests across the organization. The system eliminates the need for manual tracking via emails, Excel sheets, or registers, reducing resource wastage and providing comprehensive reporting capabilities for management.

### Project Objectives
- **Reduce Wastage**: Optimize inventory levels by tracking actual usage vs. stored quantities
- **Efficient Tracking**: Replace manual tracking methods with an automated system
- **Management Reporting**: Provide high-level reports for decision-making
- **Streamlined Approval Process**: Implement role-based approval workflows

---

## System Architecture

### Technology Stack
- **Framework**: ASP.NET Core MVC
- **Database**: SQL Server
- **Authentication**: Session-based with BCrypt password hashing
- **Frontend**: Bootstrap 5, Bootstrap Icons
- **Backend**: C# .NET

### Database Schema

#### 1. Employees Table
| Field Name | Type | Range/Constraints | Description |
|------------|------|------------------|-------------|
| EmployeeId | int | Auto-increment | Primary key, unique identifier |
| EmployeeNumber | string | 1-1000, Unique | **Login identifier**, key field |
| Name | string | Max 15 chars, no special chars | Employee full name |
| Email | string | Max 25 chars, Unique | Email address |
| PasswordHash | string | Encrypted (BCrypt) | Password in encrypted form |
| RoleId | int | Foreign key | Links to Roles table |
| Location | string | Optional | Work location |
| Grade | string | Optional | Employee grade/level |
| SuperiorId | int? | Foreign key, Nullable | Employee number of superior |
| IsActive | bool | Default: true | Account status |
| IsPendingApproval | bool | Default: false | Registration approval status |
| CreatedAt | DateTime | Auto-set | Registration timestamp |
| ModifiedAt | DateTime? | Optional | Last modification timestamp |

**Key Relationships:**
- Employee → Role (Many-to-One)
- Employee → Superior (Self-referencing, Many-to-One)
- Employee → Subordinates (One-to-Many)

#### 2. Roles Table
| Field Name | Type | Description |
|------------|------|-------------|
| RoleId | int | Primary key |
| RoleName | string | Role name (Admin, Manager, Employee, etc.) |
| Description | string | Role description |
| CanApprove | bool | Whether role can approve requests |
| ReportsToRoleId | int? | Role hierarchy - which role this reports to |
| RoleThreshold | Relationship | Links to RoleThreshold table |

**Role Hierarchy:**
- Admin (RoleId: 1) - Highest level
- Manager (RoleId: 2) - Department heads
- Employee (RoleId: 3) - Regular employees

#### 3. RoleThreshold Table
| Field Name | Type | Description |
|------------|------|-------------|
| RoleThresholdId | int | Primary key |
| RoleId | int | Foreign key to Roles |
| MaxAmount | decimal | Maximum purchase amount allowed per month |

#### 4. Categories Table
| Field Name | Type | Description |
|------------|------|-------------|
| CategoryId | int | Primary key |
| CategoryName | string | Category name |
| Description | string | Category description |

#### 5. StationeryItems Table
| Field Name | Type | Description |
|------------|------|-------------|
| ItemId | int | Primary key |
| ItemName | string | Item name |
| Description | string | Item description |
| CategoryId | int | Foreign key to Categories |
| UnitCost | decimal | Cost per unit |
| CurrentStock | int | Available quantity |
| ImagePath | string | Path to item image |
| ReorderLevel | int | Minimum stock level |

#### 6. StationeryRequests Table
| Field Name | Type | Description |
|------------|------|-------------|
| RequestId | int | Primary key |
| EmployeeId | int | Foreign key to Employees |
| SuperiorId | int? | Foreign key to Employees (approver) |
| Status | string | Pending, Approved, Rejected, Cancelled, Withdrawn, CancelPending |
| RequestDate | DateTime | Request submission date |
| TotalCost | decimal | Total cost of request |
| LastStatusChangedAt | DateTime? | Last status change timestamp |

#### 7. RequestItems Table
| Field Name | Type | Description |
|------------|------|-------------|
| RequestItemId | int | Primary key |
| RequestId | int | Foreign key to StationeryRequests |
| ItemId | int | Foreign key to StationeryItems |
| Quantity | int | Requested quantity |
| UnitCost | decimal | Cost at time of request |

#### 8. Notifications Table
| Field Name | Type | Description |
|------------|------|-------------|
| NotificationId | int | Primary key |
| EmployeeId | int | Foreign key to Employees |
| Message | string | Notification message |
| RelatedRequestId | int? | Optional link to related request |
| CreatedAt | DateTime | Notification timestamp |
| IsRead | bool | Read status |

---

## Functional Components

### 1. User Authentication & Registration

#### Registration Process
1. **Public Registration**: Anyone can register via the Register page
2. **Required Fields**:
   - Employee Number (1-1000, unique) - **Used as login identifier**
   - Name (max 15 characters, no special characters)
   - Email (max 25 characters, unique)
   - Password (min 6 characters, encrypted with BCrypt)
3. **Optional Fields**: Location, Grade
4. **Admin Approval**: New registrations require admin approval
   - Account is set to `IsPendingApproval = true` and `IsActive = false`
   - Admins receive notifications for pending registrations
   - Once approved, user can log in

#### Login Process
1. **Login Identifier**: Employee Number (not email)
2. **Authentication**: 
   - Employee Number + Password
   - Password verified using BCrypt
3. **Session Management**:
   - Session stores: EmployeeId, EmployeeName, RoleId
   - Optional "Remember Me" feature (30-day cookies)
4. **Access Control**:
   - Pending approval accounts cannot log in
   - Inactive accounts cannot log in

#### Password Management
- Users can change password after login
- Password changes trigger notifications to user and superior
- Passwords stored in encrypted form (BCrypt)

---

### 2. Role-Based Access Control

#### Roles & Permissions

**Admin (RoleId: 1)**
- Full system access
- Add/Edit/Delete: Categories, Items, Employees, Roles
- Approve/Reject registrations
- View all requests and reports
- Manage system settings

**Manager (RoleId: 2)**
- Approve/Reject stationery requests
- View reports (item costs, headcount, cumulative costs)
- View all employees in their department
- Cannot modify system settings

**Employee (RoleId: 3)**
- Create stationery requests
- View own requests and status
- Check item availability
- View eligibility details
- Withdraw/Cancel own requests
- Cannot approve requests

#### Hierarchy System
- Roles have reporting relationships (`ReportsToRoleId`)
- Employees have superiors (`SuperiorId`)
- Approval workflow follows hierarchy

---

### 3. Stationery Item Management

#### Item Display
- **Image-based Display**: Items shown with images
- **Category Filtering**: Filter items by category
- **Availability Calculation**: 
  - Available Stock = Current Stock - Reserved Stock
  - Reserved Stock = Pending + Approved requests
- **Role-based Access**: Different views for Admin vs. Employees

#### Item Operations (Admin Only)
- Create new items
- Edit item details
- Delete items
- Upload item images

---

### 4. Stationery Request System

#### Request Creation
1. Employee selects items and quantities
2. System calculates total cost
3. Checks eligibility (monthly budget limits)
4. Request assigned to superior for approval
5. Status: "Pending"

#### Request Status Flow
```
Pending → Approved/Rejected
Pending → Withdrawn (by employee)
Approved → CancelPending → Cancelled (requires superior approval)
```

#### Request Operations

**By Employee:**
- **Withdraw**: Cancel pending request (no approval needed)
- **Cancel**: Request cancellation of approved request (requires superior approval)

**By Approver (Manager/Admin):**
- **Approve**: Approve pending request
- **Reject**: Reject pending request

#### Eligibility System
- Each role has a monthly spending limit (`RoleThreshold.MaxAmount`)
- System tracks current month spending (Approved + Pending requests)
- Employees can view:
  - Maximum allowed amount
  - Current month spending
  - Available budget
  - Eligibility page shows detailed breakdown

---

### 5. Approval Workflow

#### Approval Process
1. Employee submits request
2. Request assigned to superior (based on `SuperiorId`)
3. Superior receives notification
4. Superior can Approve/Reject
5. Both employee and superior receive notifications

#### Approval Rules
- Only roles with `CanApprove = true` can approve
- Managers and Admins can approve
- Employees cannot approve
- Hierarchy determines who can approve

---

### 6. Notification System

#### Automatic Notifications
Notifications are automatically sent for:
- **Registration**: New registration pending approval
- **Request Submitted**: When employee creates request
- **Request Approved**: When superior approves request
- **Request Rejected**: When superior rejects request
- **Request Withdrawn**: When employee withdraws request
- **Request Cancelled**: When cancellation is approved
- **Password Changed**: When user changes password
- **Registration Approved**: When admin approves registration

#### Notification Features
- Real-time notifications in navbar
- Unread count badge
- Notification history page
- Click to navigate to related request
- Auto-mark as read when viewed

---

### 7. Reporting System (Manager/Admin)

#### Item Cost Report
- **Total Cost per Item**: Sum of all requests for each item
- **Unit Price**: Current item cost
- **Quantity Requested**: Total quantity across all requests
- **Headcount**: Number of unique employees requesting item
- **Total Spent**: Total cost for item
- **% of Total**: Percentage of total spending
- **Cumulative Cost**: Running total sorted by cost

#### Report Features
- Visual representation of spending
- Sortable by various metrics
- Export capabilities (future enhancement)
- Historical data tracking

---

### 8. Help System

#### Help Pages
- Q&A format documentation
- Step-by-step guides
- Feature explanations
- Troubleshooting tips
- System usage instructions

---

## Security Features

### Authentication Security
- **Password Encryption**: BCrypt hashing (one-way encryption)
- **Session Management**: Secure session storage
- **Remember Me**: Secure cookie-based authentication
- **Access Control**: Role-based authorization filters

### Authorization Filters
- **RequireLoginAttribute**: Ensures user is logged in
- **RequireAdminAttribute**: Ensures user is admin
- **Controller-level**: Applied to entire controllers
- **Action-level**: Applied to specific actions

### Data Validation
- **Input Validation**: Server-side and client-side
- **SQL Injection Prevention**: Parameterized queries (Entity Framework)
- **XSS Prevention**: Razor encoding
- **CSRF Protection**: Anti-forgery tokens

---

## User Interface Features

### Design Principles
- **Responsive Design**: Works on desktop, tablet, mobile
- **Modern UI**: Bootstrap 5 with custom styling
- **Accessibility**: Semantic HTML, ARIA labels
- **User Experience**: Intuitive navigation, clear feedback

### Key UI Components
- **Gradient Buttons**: Styled action buttons
- **Enhanced Tables**: Gradient headers, hover effects
- **Card Layouts**: Clean card-based design
- **Alert System**: Success/Error/Info messages
- **Loading States**: Visual feedback for actions

---

## Database Relationships

```
Employees
├── Role (Many-to-One)
├── Superior (Self-referencing, Many-to-One)
├── Subordinates (One-to-Many)
├── StationeryRequests (One-to-Many)
└── Notifications (One-to-Many)

Roles
├── RoleThreshold (One-to-One)
├── ReportsTo (Self-referencing, Many-to-One)
└── DirectReports (One-to-Many)

StationeryRequests
├── Employee (Many-to-One)
├── Superior (Many-to-One)
└── RequestItems (One-to-Many)

RequestItems
├── Request (Many-to-One)
└── Item (Many-to-One)

StationeryItems
└── Category (Many-to-One)
```

---

## API Endpoints

### Account Controller
- `GET /Account/Login` - Login page
- `POST /Account/Login` - Authenticate user
- `GET /Account/Register` - Registration page
- `POST /Account/Register` - Submit registration
- `GET /Account/ChangePassword` - Change password page
- `POST /Account/ChangePassword` - Update password
- `GET /Account/Logout` - Logout user
- `GET /Account/AccessDenied` - Access denied page

### Employees Controller
- `GET /Employees` - List all employees (Admin/Manager only)
- `GET /Employees/Details/{id}` - Employee details (Admin/Manager can see all, Employees can only see themselves)
- `GET /Employees/Create` - Create employee (Admin only)
- `POST /Employees/Create` - Submit new employee (Admin only)
- `GET /Employees/Edit/{id}` - Edit employee (Admin only)
- `POST /Employees/Edit/{id}` - Update employee (Admin only)
- `GET /Employees/Delete/{id}` - Delete confirmation (Admin only)
- `POST /Employees/Delete/{id}` - Delete employee (Admin only)

### StationeryItems Controller
- `GET /StationeryItems` - List all items
- `GET /StationeryItems/Details/{id}` - Item details
- `GET /StationeryItems/Eligibility` - User eligibility
- `GET /StationeryItems/Create` - Create item (Admin)
- `POST /StationeryItems/Create` - Submit new item (Admin)
- `GET /StationeryItems/Edit/{id}` - Edit item (Admin)
- `POST /StationeryItems/Edit/{id}` - Update item (Admin)
- `GET /StationeryItems/Delete/{id}` - Delete confirmation (Admin)
- `POST /StationeryItems/Delete/{id}` - Delete item (Admin)

### StationeryRequests Controller
- `GET /StationeryRequests` - List requests (role-based)
- `GET /StationeryRequests/Details/{id}` - Request details
- `GET /StationeryRequests/Create` - Create request
- `POST /StationeryRequests/Create` - Submit request
- `POST /StationeryRequests/Approve/{id}` - Approve request
- `POST /StationeryRequests/Reject/{id}` - Reject request
- `POST /StationeryRequests/Withdraw/{id}` - Withdraw request
- `POST /StationeryRequests/Cancel/{id}` - Request cancellation
- `POST /StationeryRequests/ApproveCancel/{id}` - Approve cancellation

### Reports Controller
- `GET /Reports` - Item cost report (Manager/Admin)

### Notifications Controller
- `GET /Notifications` - Notification list
- `GET /Notifications/Go/{id}` - Navigate to related item

---

## Configuration

### Connection String
Located in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=StationeryDB;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### Environment Variables
Can override connection string:
```bash
StationeryDB_ConnectionString="Server=...;Database=...;..."
```

### Session Configuration
- **Timeout**: 1 hour
- **Cookie**: HttpOnly, Secure (HTTPS in production)
- **Storage**: In-memory cache

---

## Deployment Considerations

### Production Checklist
- [ ] Update connection string for production database
- [ ] Enable HTTPS (set `Cookie.SecurePolicy = Always`)
- [ ] Configure proper session storage (Redis/SQL Server)
- [ ] Set up backup strategy
- [ ] Configure logging
- [ ] Review security settings
- [ ] Test all workflows
- [ ] Set up monitoring

### Performance Optimization
- Database indexing on frequently queried fields
- Caching for static data
- Lazy loading for navigation properties
- Pagination for large lists

---

## Future Enhancements

### Planned Features
1. **Email Notifications**: Send email notifications in addition to in-app
2. **Export Reports**: PDF/Excel export for reports
3. **Advanced Analytics**: Trend analysis, forecasting
4. **Mobile App**: Native mobile application
5. **Bulk Operations**: Bulk approve/reject requests
6. **Audit Log**: Track all system changes
7. **Multi-language Support**: Internationalization
8. **API Integration**: RESTful API for external systems

---

## Troubleshooting

### Common Issues

**Login Issues**
- Verify Employee Number is correct (1-1000)
- Check if account is pending approval
- Verify account is active
- Check password is correct

**Registration Issues**
- Employee Number must be unique and between 1-1000
- Name must be max 15 characters, no special characters
- Email must be unique and max 25 characters
- Password must be at least 6 characters

**Database Issues**
- Check connection string
- Verify migrations are applied
- Check SQL Server is running
- Review Program.cs for automatic column addition

---

## Support & Maintenance

### System Requirements
- .NET 6.0 or higher
- SQL Server 2016 or higher
- IIS or Kestrel web server
- Modern web browser (Chrome, Firefox, Edge, Safari)

### Maintenance Tasks
- Regular database backups
- Monitor session storage
- Review and clean old notifications
- Update dependencies regularly
- Review security logs

---

## Version History

### Version 1.0 (Current)
- Initial release
- Core functionality implemented
- Role-based access control
- Request/approval workflow
- Reporting system
- Notification system
- Admin approval for registrations

---

## Contact & Documentation

For additional documentation, see:
- `README_SETUP.md` - Setup instructions
- `TROUBLESHOOTING.md` - Troubleshooting guide
- `IMPROVEMENTS_SUMMARY.md` - Recent improvements

---

**Document Version**: 1.0  
**Last Updated**: 2025  
**Project**: HMT Technologies Stationery Management System

